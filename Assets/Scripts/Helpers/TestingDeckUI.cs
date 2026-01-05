// using UnityEngine;
// using System.Collections.Generic;

// public class TestingDeckUI : MonoBehaviour
// {
//     private DeckManager deckManager;
//     private bool showMenu = false;
//     private Vector2 scrollPos;

//     private List<DeckEntry> tempPlayerDeck;
//     private List<DeckEntry> tempAIDeck;
    
//     private int tempSkillLevel = 10;

//     private enum EditMode { Player, AI }
//     private EditMode currentMode = EditMode.Player;

//     void Start()
//     {
//         deckManager = GetComponent<DeckManager>();
//         LoadFromManager();
        
//         // Load current persisted skill level
//         tempSkillLevel = StockfishManager.persistedSkillLevel;
//     }

//     void LoadFromManager()
//     {
//         if (deckManager.playerDeckSetup != null)
//             tempPlayerDeck = new List<DeckEntry>(deckManager.playerDeckSetup);
//         else 
//             tempPlayerDeck = new List<DeckEntry>();

//         if (deckManager.aiDeckSetup != null)
//             tempAIDeck = new List<DeckEntry>(deckManager.aiDeckSetup);
//         else 
//             tempAIDeck = new List<DeckEntry>();
//     }

//     void OnGUI()
//     {
//         // Toggle Button
//         if (GUI.Button(new Rect(10, 10, 150, 30), showMenu ? "Close Deck Tool" : "Open Deck Tool"))
//         {
//             showMenu = !showMenu;
//             if (showMenu) LoadFromManager();
//         }

//         if (!showMenu) return;

//         // Background Box
//         GUI.Box(new Rect(10, 50, 280, 520), "Deck Configuration");

//         // --- MODE TABS ---
//         GUILayout.BeginArea(new Rect(20, 80, 260, 40));
//         GUILayout.BeginHorizontal();
//         if (GUILayout.Button("Edit Player Deck", currentMode == EditMode.Player ? GUI.skin.box : GUI.skin.button)) 
//             currentMode = EditMode.Player;
//         if (GUILayout.Button("Edit AI Deck", currentMode == EditMode.AI ? GUI.skin.box : GUI.skin.button)) 
//             currentMode = EditMode.AI;
//         GUILayout.EndHorizontal();
//         GUILayout.EndArea();

//         List<DeckEntry> currentList = (currentMode == EditMode.Player) ? tempPlayerDeck : tempAIDeck;
        
//         // Count totals for validation
//         int currentListTotal = GetTotalCount(currentList);
//         string headerTitle = (currentMode == EditMode.Player) ? "Player Cards" : "AI Cards";
//         headerTitle += $" ({currentListTotal})";

//         // --- PRESET BUTTONS (AI Only) ---
//         if (currentMode == EditMode.AI)
//         {
//             GUI.Box(new Rect(300, 50, 120, 200), "AI Presets");
//             GUILayout.BeginArea(new Rect(310, 80, 100, 160));
//             if (GUILayout.Button("Easy")) ApplyPreset("easy");
//             if (GUILayout.Button("Medium")) ApplyPreset("medium");
//             if (GUILayout.Button("Hard")) ApplyPreset("hard");
//             if (GUILayout.Button("Impossible")) ApplyPreset("impossible");
//             GUILayout.EndArea();
//         }

//         // --- SCROLL AREA ---
//         GUILayout.BeginArea(new Rect(20, 130, 260, 310));
//         scrollPos = GUILayout.BeginScrollView(scrollPos);

//         GUILayout.Label(headerTitle);
//         for (int i = 0; i < currentList.Count; i++)
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.Label(currentList[i].pieceType + " (" + currentList[i].count + ")", GUILayout.Width(120));
            
//             if (GUILayout.Button("-", GUILayout.Width(30))) ModifyCount(currentList, i, -1);
//             if (GUILayout.Button("+", GUILayout.Width(30))) ModifyCount(currentList, i, 1);
//             GUILayout.EndHorizontal();
//         }

//         GUILayout.Space(15);
//         GUILayout.Label("Add New Card:");

//         foreach (var kvp in deckManager.cardLibrary)
//         {
//             string type = kvp.Key;
//             if (!IsTypeInList(currentList, type))
//             {
//                 if (GUILayout.Button("Add " + type.ToUpper()))
//                 {
//                     currentList.Add(new DeckEntry { pieceType = type, count = 1 });
//                 }
//             }
//         }

//         GUILayout.EndScrollView();
//         GUILayout.EndArea();

//         // --- SKILL LEVEL SLIDER ---
//         GUILayout.BeginArea(new Rect(20, 450, 260, 60));
//         GUILayout.Label($"Stockfish Skill Level: {tempSkillLevel}");
//         tempSkillLevel = (int)GUILayout.HorizontalSlider(tempSkillLevel, 0, 20);
//         GUILayout.EndArea();

//         // --- APPLY BUTTON WITH VALIDATION ---
//         int playerTotal = GetTotalCount(tempPlayerDeck);
//         int aiTotal = GetTotalCount(tempAIDeck);
//         bool isValid = playerTotal >= 6 && aiTotal >= 6;

//         if (isValid)
//         {
//             if (GUI.Button(new Rect(20, 510, 260, 40), "Apply & Restart Game"))
//             {
//                 ApplyAndRestart();
//             }
//         }
//         else
//         {
//             // Disable button and show why
//             GUI.enabled = false;
//             string status = "";
//             if (playerTotal < 6) status = $"Player needs {6 - playerTotal} more";
//             else if (aiTotal < 6) status = $"AI needs {6 - aiTotal} more";
            
//             GUI.Button(new Rect(20, 510, 260, 40), status);
//             GUI.enabled = true;
//         }
//     }

//     int GetTotalCount(List<DeckEntry> list)
//     {
//         int total = 0;
//         foreach (var entry in list) total += entry.count;
//         return total;
//     }

//     void ApplyPreset(string difficulty)
//     {
//         tempAIDeck.Clear();
//         switch (difficulty)
//         {
//             case "easy":
//                 tempAIDeck.Add(new DeckEntry { pieceType = "pawn", count = 8 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "knight", count = 2 });
//                 break;
//             case "medium":
//                 tempAIDeck.Add(new DeckEntry { pieceType = "pawn", count = 4 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "knight", count = 2 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "bishop", count = 2 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "rook", count = 2 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "queen", count = 1 });
//                 break;
//             case "hard":
//                 tempAIDeck.Add(new DeckEntry { pieceType = "knight", count = 2 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "bishop", count = 2 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "rook", count = 4 });
//                 tempAIDeck.Add(new DeckEntry { pieceType = "queen", count = 2 });
//                 break;
//             case "impossible":
//                 tempAIDeck.Add(new DeckEntry { pieceType = "queen", count = 40 });
//                 break;
//         }
//     }

//     void ModifyCount(List<DeckEntry> list, int index, int amount)
//     {
//         DeckEntry entry = list[index];
//         entry.count += amount;
//         if (entry.count <= 0) list.RemoveAt(index);
//         else list[index] = entry; 
//     }

//     bool IsTypeInList(List<DeckEntry> list, string type)
//     {
//         foreach(var e in list) if (e.pieceType == type) return true;
//         return false;
//     }

//     void ApplyAndRestart()
//     {
//         DeckManager.savedPlayerDeck = new List<DeckEntry>(tempPlayerDeck);
//         DeckManager.savedAIDeck = new List<DeckEntry>(tempAIDeck);
        
//         StockfishManager.persistedSkillLevel = tempSkillLevel;

//         UnityEngine.SceneManagement.SceneManager.LoadScene(
//             UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
//         );
//     }
// }