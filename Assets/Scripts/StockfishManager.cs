using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
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
    [Tooltip("Path to stockfish.exe - can be absolute or relative to game root")]
    public string stockfishPath = "stockfish-windows-x86-64-avx2.exe";
    
    [Tooltip("Skill level 0-20, where 20 is strongest")]
    [Range(0, 20)]
    public int skillLevel = 5;
    
    [Tooltip("Time limit for Stockfish to think (milliseconds)")]
    public int thinkingTime = 1000;

    void Start()
    {
        InitializeStockfish();
    }

    void InitializeStockfish()
    {
        try
        {
            // Try to find Stockfish in multiple locations
            string exePath = FindStockfishExecutable();
            
            if (string.IsNullOrEmpty(exePath))
            {
                UnityEngine.Debug.LogError($"Stockfish executable not found!\n\nPlease download Stockfish from https://stockfishchess.org/download/\nRename the executable to 'stockfish.exe'\nPlace it in the folder: {Path.Combine(Application.dataPath, "StreamingAssets")}");
                return;
            }

            // Create process
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            stockfishProcess = new Process { StartInfo = startInfo };
            stockfishProcess.Start();

            stockfishInput = stockfishProcess.StandardInput;
            stockfishOutput = stockfishProcess.StandardOutput;

            // Initialize UCI protocol
            SendCommand("uci");
            WaitForResponse("uciok");
            
            // Set skill level
            SendCommand($"setoption name Skill Level value {skillLevel}");
            
            SendCommand("isready");
            WaitForResponse("readyok");
            
            isReady = true;
            UnityEngine.Debug.Log($"Stockfish initialized successfully at skill level {skillLevel}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to initialize Stockfish: {e.Message}");
        }
    }

    string FindStockfishExecutable()
    {
        // Try multiple possible locations
        string[] possiblePaths = new string[]
        {
            stockfishPath, // User-specified path
            Path.Combine(Application.dataPath, "StreamingAssets", "stockfish.exe"),
            Path.Combine(Application.dataPath, "..", "stockfish.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "stockfish.exe"),
            "stockfish.exe" // Assume it's in PATH
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                UnityEngine.Debug.Log($"Found Stockfish at: {path}");
                return path;
            }
        }

        return null;
    }

    void SendCommand(string command)
    {
        if (stockfishInput != null)
        {
            stockfishInput.WriteLine(command);
            stockfishInput.Flush();
        }
    }

    void WaitForResponse(string expectedResponse)
    {
        string line;
        while ((line = stockfishOutput.ReadLine()) != null)
        {
            if (line.Contains(expectedResponse))
            {
                break;
            }
        }
    }

    public void GetBestMove(string fenPosition, Action<string> callback)
    {
        if (!isReady)
        {
            UnityEngine.Debug.LogWarning("Stockfish not ready!");
            callback?.Invoke("");
            return;
        }

        StartCoroutine(GetBestMoveAsync(fenPosition, callback));
    }

    IEnumerator GetBestMoveAsync(string fenPosition, Action<string> callback)
    {
        moveReceived = false;
        lastBestMove = "";

        // Send position and request move
        SendCommand($"position fen {fenPosition}");
        SendCommand($"go movetime {thinkingTime}");

        // Start reading output in background
        StartCoroutine(ReadStockfishOutput());

        // Wait for move to be received
        float timeout = (thinkingTime / 1000f) + 5f; // Add 5 second buffer
        float elapsed = 0f;
        
        while (!moveReceived && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!moveReceived)
        {
            UnityEngine.Debug.LogWarning("Stockfish did not respond in time");
        }

        callback?.Invoke(lastBestMove);
    }

    IEnumerator ReadStockfishOutput()
    {
        // Read in a coroutine to avoid blocking
        while (!moveReceived)
        {
            if (stockfishOutput != null && !stockfishOutput.EndOfStream)
            {
                string line = stockfishOutput.ReadLine();
                
                if (line != null && line.StartsWith("bestmove"))
                {
                    // Parse: "bestmove e2e4" or "bestmove e2e4 ponder e7e5"
                    string[] parts = line.Split(' ');
                    if (parts.Length >= 2)
                    {
                        lastBestMove = parts[1];
                        moveReceived = true;
                        UnityEngine.Debug.Log($"Stockfish suggests: {lastBestMove}");
                    }
                }
            }
            yield return null;
        }
    }

    void OnDestroy()
    {
        // Clean up Stockfish process
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            SendCommand("quit");
            stockfishProcess.WaitForExit(1000);
            
            if (!stockfishProcess.HasExited)
            {
                stockfishProcess.Kill();
            }
            
            stockfishProcess.Close();
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
