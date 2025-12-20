using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class StockfishManager : MonoBehaviour
{
    private Process stockfishProcess;
    private StreamWriter stockfishInput;
    private StreamReader stockfishOutput;
    private bool isReady = false;
    private string lastBestMove = "";
    private bool moveReceived = false;

    [Header("Stockfish Settings")]
    public string stockfishPath = "stockfish.exe"; // Make sure this matches your file name!
    [Range(0, 20)]
    public int skillLevel = 10;
    public int thinkingTime = 500; // ms

    void Start()
    {
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
            
            isReady = true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start Stockfish: {e.Message}");
        }
    }

    void SendCommand(string command)
    {
        if (stockfishInput != null)
        {
            stockfishInput.WriteLine(command);
            stockfishInput.Flush();
        }
    }

    public void GetBestMove(string fenPosition, Action<string> callback)
    {
        if (!isReady) return;
        StartCoroutine(GetBestMoveAsync(fenPosition, callback));
    }

    IEnumerator GetBestMoveAsync(string fenPosition, Action<string> callback)
    {
        moveReceived = false;
        lastBestMove = "";

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

        callback?.Invoke(lastBestMove);
    }

    IEnumerator ReadStockfishOutput()
    {
        while (!moveReceived)
        {
            if (stockfishOutput != null && !stockfishOutput.EndOfStream)
            {
                string line = stockfishOutput.ReadLine();
                if (line != null && line.StartsWith("bestmove"))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length >= 2)
                    {
                        lastBestMove = parts[1];
                        moveReceived = true;
                    }
                }
            }
            yield return null;
        }
    }

    void OnDestroy()
    {
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            SendCommand("quit");
            stockfishProcess.Kill();
            stockfishProcess.Close();
        }
    }
}