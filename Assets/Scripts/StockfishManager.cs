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

    // Persist skill level across scene reloads
    public static int persistedSkillLevel = 10;

    [Header("Stockfish Settings")]
    public string stockfishPath = "fairy-stockfish.exe"; // CHANGED: Default to fairy-stockfish
    public string variantConfig = "variants.ini";        // NEW: Path to your variants file
    public string variantName = "cardchess";             // NEW: Name of the variant in the .ini
    
    [Range(0, 20)]
    public int skillLevel = 10;
    public int thinkingTime = 500; // ms
    
    [Header("Debug")]
    public bool debugMode = true;

    void Start()
    {
        skillLevel = persistedSkillLevel;
        InitializeStockfish();
    }

    void InitializeStockfish()
    {
        try
        {
            // Path to StreamingAssets
            string folderPath = Application.streamingAssetsPath;
            string exePath = Path.Combine(folderPath, stockfishPath);
            string variantPath = Path.Combine(folderPath, variantConfig);
            
            if (!File.Exists(exePath))
            {
                UnityEngine.Debug.LogError("Fairy-Stockfish not found at: " + exePath);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = folderPath, // Set working dir so it finds local files easier
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            stockfishProcess = new Process { StartInfo = startInfo };
            stockfishProcess.Start();
            stockfishInput = stockfishProcess.StandardInput;
            stockfishOutput = stockfishProcess.StandardOutput;

            // --- FAIRY STOCKFISH INITIALIZATION SEQUENCE ---
            SendCommand("uci");
            
            // 1. Load the variants file
            // Note: If spaces exist in path, might need quotes, but usually UCI handles basic paths
            SendCommand($"setoption name VariantPath value {variantConfig}");
            
            // 2. Select the specific variant
            SendCommand($"setoption name UCI_Variant value {variantName}");
            
            // 3. Standard Settings
            SendCommand($"setoption name Skill Level value {skillLevel}");
            
            SendCommand("isready");
            // -----------------------------------------------
            
            if (debugMode) UnityEngine.Debug.Log($"Fairy-Stockfish Initialized. Variant: {variantName}");
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
                    // Debug output from engine can be helpful to see if variant loaded
                    // if (debugMode) UnityEngine.Debug.Log("SF: " + line);

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