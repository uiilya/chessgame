using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;

public class StockfishResult
{
    public bool success;
    public string bestmove;
    public int? mate;       
    public int? evaluation; 
}

public class StockfishManager : MonoBehaviour
{
    private Process stockfishProcess;
    private StreamWriter stockfishInput;
    private StreamReader stockfishOutput;

    private string lastBestMove = "";
    private int? lastMate = null;
    private int? lastEval = null;
    private bool moveReceived = false;

    // ADDED: Static variable to persist skill level across scene reloads
    public static int persistedSkillLevel = 10;

    [Header("Stockfish Settings")]
    public string stockfishPath = "stockfish.exe"; 
    [Range(0, 20)]
    public int skillLevel = 10;
    public int thinkingTime = 500; // ms
    
    [Header("Debug")]
    public bool debugMode = true;

    void Start()
    {
        // Sync with persisted value from Debug UI
        skillLevel = persistedSkillLevel;
        InitializeStockfish();
    }

    void InitializeStockfish()
    {
        try
        {
            string exePath = Path.Combine(Application.streamingAssetsPath, stockfishPath);
            
            if (!File.Exists(exePath))
            {
                UnityEngine.Debug.LogError("Stockfish not found at: " + exePath);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            stockfishProcess = new Process { StartInfo = startInfo };
            stockfishProcess.Start();
            stockfishInput = stockfishProcess.StandardInput;
            stockfishOutput = stockfishProcess.StandardOutput;

            SendCommand("uci");
            SendCommand($"setoption name Skill Level value {skillLevel}");
            SendCommand("isready");
            
            if (debugMode) UnityEngine.Debug.Log($"Stockfish Initialized. Skill Level: {skillLevel}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Stockfish Init Error: " + e.Message);
        }
    }

    public void SendCommand(string command)
    {
        if (stockfishInput != null)
        {
            stockfishInput.WriteLine(command);
            stockfishInput.Flush();
        }
    }

    public void GetBestMove(string fenPosition, Action<StockfishResult> callback)
    {
        StartCoroutine(GetBestMoveAsync(fenPosition, callback));
    }

    IEnumerator GetBestMoveAsync(string fenPosition, Action<StockfishResult> callback)
    {
        moveReceived = false;
        lastBestMove = "";
        lastMate = null;
        lastEval = null;

        if (debugMode) UnityEngine.Debug.Log($"[Stockfish] Sending FEN: {fenPosition}");

        SendCommand($"position fen {fenPosition}");
        SendCommand($"go movetime {thinkingTime}");

        StartCoroutine(ReadStockfishOutput());

        float timeout = (thinkingTime / 1000f) + 2f;
        float elapsed = 0f;
        
        while (!moveReceived && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (debugMode)
        {
            string scoreType = lastMate.HasValue ? $"Mate in {lastMate}" : $"CP {lastEval}";
            UnityEngine.Debug.Log($"[Stockfish] BestMove: {lastBestMove} | Score: {scoreType}");
        }

        StockfishResult result = new StockfishResult
        {
            success = !string.IsNullOrEmpty(lastBestMove),
            bestmove = lastBestMove,
            mate = lastMate,
            evaluation = lastEval
        };

        callback?.Invoke(result);
    }

    IEnumerator ReadStockfishOutput()
    {
        while (!moveReceived)
        {
            if (stockfishOutput != null)
            {
                string line = stockfishOutput.ReadLine(); 
                
                if (line != null)
                {
                    if (line.StartsWith("info") && line.Contains("score"))
                    {
                        ParseScore(line);
                    }

                    if (line.StartsWith("bestmove"))
                    {
                        string[] parts = line.Split(' ');
                        if (parts.Length >= 2)
                        {
                            lastBestMove = parts[1];
                            moveReceived = true;
                        }
                    }
                }
                else
                {
                    yield return null; 
                }
            }
            yield return null;
        }
    }

    void ParseScore(string line)
    {
        if (line.Contains("mate"))
        {
            var match = Regex.Match(line, @"mate\s+(-?\d+)");
            if (match.Success)
            {
                lastMate = int.Parse(match.Groups[1].Value);
            }
        }
        else if (line.Contains("cp"))
        {
            var match = Regex.Match(line, @"cp\s+(-?\d+)");
            if (match.Success)
            {
                lastEval = int.Parse(match.Groups[1].Value);
            }
        }
    }

    void OnDestroy()
    {
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            SendCommand("quit");
            stockfishProcess.Close();
        }
    }
}