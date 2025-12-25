// Core/MainMenu.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BombermanGame.src.MVC.Controllers;
using BombermanGame.src.UI;
using BombermanGame.src.Models.Entities;
using BombermanGame.src.Patterns.Repository;
using BombermanGame.src.Utils;
using BombermanGame.src.Network;

namespace BombermanGame.src.Core
{
	public class MainMenu
	{
		private UserRepository _userRepository;
		private PreferencesRepository _preferencesRepository;
		private ScoreRepository _scoreRepository;
		private StatsRepository _statsRepository;
		private SignalRClient? _signalRClient;
		private LobbyDisplay _lobbyDisplay;

		public MainMenu()
		{
			_userRepository = new UserRepository();
			_preferencesRepository = new PreferencesRepository();
			_scoreRepository = new ScoreRepository();
			_statsRepository = new StatsRepository();
			_lobbyDisplay = new LobbyDisplay();
		}

		public void Show()
		{
			while (true)
			{
				Console.Clear();
				DisplayLogo();

				Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                         MAIN MENU                            ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
				Console.WriteLine("\n1. Login");
				Console.WriteLine("2. Register");
				Console.WriteLine("3. View Leaderboard");
				Console.WriteLine("4. Exit");
				Console.Write("\nSelect option: ");

				string? choice = Console.ReadLine();

				switch (choice)
				{
					case "1":
						LoginMenu();
						break;
					case "2":
						RegisterMenu();
						break;
					case "3":
						ShowLeaderboard();
						break;
					case "4":
						Console.WriteLine("\nThank you for playing!");
						return;
					default:
						Console.WriteLine("\nInvalid option!");
						Console.ReadKey();
						break;
				}
			}
		}

		private void DisplayLogo()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(@"
    ╔══════════════════════════════════════════════════════════════╗
    ║  ____                  _                                     ║
    ║ |  _ \                | |                                    ║
    ║ | |_) | ___  _ __ ___ | |__   ___ _ __ _ __ ___   __ _ _ __  ║
    ║ |  _ < / _ \| '_ ` _ \| '_ \ / _ \ '__| '_ ` _ \ / _` | '_ \ ║
    ║ | |_) | (_) | | | | | | |_) |  __/ |  | | | | | | (_| | | | |║
    ║ |____/ \___/|_| |_| |_|_.__/ \___|_|  |_| |_| |_|\__,_|_| |_|║
    ║                                                                ║
    ╚══════════════════════════════════════════════════════════════╝
            ");
			Console.ResetColor();
		}

		private void LoginMenu()
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                           LOGIN                              ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			Console.Write("Username: ");
			string? username = Console.ReadLine();

			Console.Write("Password: ");
			string? password = ReadPassword();

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				Console.WriteLine("\nUsername and password cannot be empty!");
				Console.ReadKey();
				return;
			}

			var user = _userRepository.GetByUsername(username);
			if (user != null && PasswordHelper.VerifyPassword(password, user.PasswordHash))
			{
				Console.WriteLine("\n✓ Login successful!");
				GameManager.Instance.CurrentUserId = user.Id;
				Thread.Sleep(1000);
				GameMenu(user);
			}
			else
			{
				Console.WriteLine("\n✗ Invalid username or password!");
				Console.ReadKey();
			}
		}

		private void RegisterMenu()
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                        REGISTER                              ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			Console.Write("Username: ");
			string? username = Console.ReadLine();

			if (string.IsNullOrEmpty(username))
			{
				Console.WriteLine("\nUsername cannot be empty!");
				Console.ReadKey();
				return;
			}

			if (_userRepository.UsernameExists(username))
			{
				Console.WriteLine("\n✗ Username already exists!");
				Console.ReadKey();
				return;
			}

			Console.Write("Password: ");
			string? password = ReadPassword();

			Console.Write("\nConfirm Password: ");
			string? confirmPassword = ReadPassword();

			if (password != confirmPassword)
			{
				Console.WriteLine("\n\n✗ Passwords do not match!");
				Console.ReadKey();
				return;
			}

			if (string.IsNullOrEmpty(password) || password.Length < 6)
			{
				Console.WriteLine("\n\n✗ Password must be at least 6 characters!");
				Console.ReadKey();
				return;
			}

			var user = new User
			{
				Username = username,
				PasswordHash = PasswordHelper.HashPassword(password),
				CreatedAt = DateTime.Now
			};

			_userRepository.Add(user);

			_preferencesRepository.Add(new PlayerPreference
			{
				UserId = user.Id,
				Theme = "Desert",
				SoundEnabled = true
			});

			_statsRepository.Add(new GameStatistic
			{
				UserId = user.Id,
				Wins = 0,
				Losses = 0,
				TotalGames = 0
			});

			Console.WriteLine("\n\n✓ Registration successful!");
			Console.ReadKey();
		}

		private void GameMenu(User user)
		{
			while (true)
			{
				Console.Clear();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine($"║ Welcome, {user.Username,-50} ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

				var stats = _statsRepository.GetByUserId(user.Id);
				if (stats != null)
				{
					Console.WriteLine($"Games Played: {stats.TotalGames} | Wins: {stats.Wins} | Losses: {stats.Losses}");
					if (stats.TotalGames > 0)
					{
						double winRate = (double)stats.Wins / stats.TotalGames * 100;
						Console.WriteLine($"Win Rate: {winRate:F1}%\n");
					}
				}

				Console.WriteLine("1. Start Single Player Game");
				Console.WriteLine("2. Start Two Player Game (Local)");
				Console.WriteLine("3. Online Multiplayer (SignalR)");
				Console.WriteLine("4. View My Scores");
				Console.WriteLine("5. Settings");
				Console.WriteLine("6. Logout");
				Console.Write("\nSelect option: ");

				string? choice = Console.ReadLine();

				switch (choice)
				{
					case "1":
						StartGame(user, true);
						break;
					case "2":
						StartGame(user, false);
						break;
					case "3":
						ShowOnlineMultiplayerMenu(user);
						break;
					case "4":
						ShowMyScores(user);
						break;
					case "5":
						SettingsMenu(user);
						break;
					case "6":
						return;
					default:
						Console.WriteLine("\nInvalid option!");
						Console.ReadKey();
						break;
				}
			}
		}

		private void ShowOnlineMultiplayerMenu(User user)
		{
			while (true)
			{
				Console.Clear();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║           ONLINE MULTIPLAYER (SignalR)                       ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

				Console.WriteLine("1. Connect to Server");
				Console.WriteLine("2. Browse Rooms");
				Console.WriteLine("3. Create Room");
				Console.WriteLine("4. Quick Join");
				Console.WriteLine("5. Disconnect");
				Console.WriteLine("6. Back to Menu");
				Console.Write("\nSelect option: ");

				string? choice = Console.ReadLine();

				switch (choice)
				{
					case "1":
						ConnectToServerAsync(user).Wait();
						break;
					case "2":
						if (_signalRClient != null && _signalRClient.IsConnected)
						{
							BrowseRooms(user);
						}
						else
						{
							ConsoleUI.ShowError("Not connected to server!");
							Thread.Sleep(2000);
						}
						break;
					case "3":
						if (_signalRClient != null && _signalRClient.IsConnected)
						{
							CreateRoom(user);
						}
						else
						{
							ConsoleUI.ShowError("Not connected to server!");
							Thread.Sleep(2000);
						}
						break;
					case "4":
						if (_signalRClient != null && _signalRClient.IsConnected)
						{
							QuickJoin(user);
						}
						else
						{
							ConsoleUI.ShowError("Not connected to server!");
							Thread.Sleep(2000);
						}
						break;
					case "5":
						if (_signalRClient != null)
						{
							_signalRClient.DisconnectAsync().Wait();
							_signalRClient = null;
							ConsoleUI.ShowSuccess("Disconnected from server");
							Thread.Sleep(1500);
						}
						break;
					case "6":
						return;
					default:
						Console.WriteLine("\nInvalid option!");
						Console.ReadKey();
						break;
				}
			}
		}

		private async Task ConnectToServerAsync()
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                   CONNECT TO SERVER                          ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			Console.WriteLine("Enter server URL:");
			Console.WriteLine("  1. localhost (http://localhost:5274)");
			Console.WriteLine("  2. Custom URL");
			Console.Write("\nChoice (1-2): ");

			string? choice = Console.ReadLine();
			string serverUrl;

			if (choice == "1")
			{
				serverUrl = "http://localhost:5274";
			}
			else
			{
				Console.Write("Enter server URL: ");
				serverUrl = Console.ReadLine() ?? "http://localhost:5274";
			}

			_lobbyDisplay.ShowLoadingAnimation("Connecting to server", 2000);

			_signalRClient = new SignalRClient();

			bool connected = await _signalRClient.ConnectAsync(serverUrl);

			if (connected)
			{
				ConsoleUI.ShowSuccess($"Connected to {serverUrl}");
				Console.WriteLine($"Connection ID: {_signalRClient.ConnectionId}");
				Thread.Sleep(2000);
			}
			else
			{
				ConsoleUI.ShowError("Failed to connect to server");
				_signalRClient = null;
				Thread.Sleep(2000);
			}
		}

		private async Task ConnectToServerAsync(User user)
		{
			await ConnectToServerAsync();
		}
		private void BrowseRooms(User user)
		{
			// Bağlantı kontrolü
			if (_signalRClient == null || !_signalRClient.IsConnected)
			{
				ConsoleUI.ShowError("Not connected to server!");
				Thread.Sleep(2000);
				return;
			}

			_lobbyDisplay.ShowLoadingAnimation("Loading rooms", 1000);

			// Veriyi çekme
			var roomListTask = _signalRClient.GetRoomListAsync();
			roomListTask.Wait();
			var roomListResponse = roomListTask.Result;

			if (roomListResponse == null)
			{
				ConsoleUI.ShowError("Failed to get room list (Null response)");
				Thread.Sleep(2000);
				return;
			}

			// JSON Dönüşümü
			var jsonElement = (System.Text.Json.JsonElement)roomListResponse;
			System.Text.Json.JsonElement roomsArray;

			// --- KRİTİK DÜZELTME: Hem 'rooms' hem 'Rooms' kontrolü ---
			if (jsonElement.TryGetProperty("rooms", out roomsArray))
			{
				// Küçük harf bulundu, sorun yok
			}
			else if (jsonElement.TryGetProperty("Rooms", out roomsArray))
			{
				// Büyük harf bulundu, sorun yok
			}
			else
			{
				// İkisi de yoksa hatayı ekrana bas ama çökme
				ConsoleUI.ShowError("List not found in JSON response!");
				Console.WriteLine($"\n[DEBUG] Server sent keys: {jsonElement.ToString()}"); // Hata ayıklama için
				Thread.Sleep(5000);
				return;
			}
			// -----------------------------------------------------------

			var rooms = new System.Collections.Generic.List<RoomInfo>();

			try
			{
				foreach (var roomElement in roomsArray.EnumerateArray())
				{
					// Güvenli okuma yardımcı metotları (Aşağıdaki helper'ları da eklemeyi unutma!)
					var room = new RoomInfo
					{
						Id = GetSafeString(roomElement, "id", "Id"),
						Name = GetSafeString(roomElement, "name", "Name"),
						Theme = GetSafeString(roomElement, "theme", "Theme", "Desert"),
						State = GetSafeString(roomElement, "state", "State", "Waiting"),
						CurrentPlayers = GetSafeInt(roomElement, "currentPlayers", "CurrentPlayers"),
						MaxPlayers = GetSafeInt(roomElement, "maxPlayers", "MaxPlayers")
					};

					// Oyuncu isimleri listesini okuma
					if (TryGetPropertyCaseInsensitive(roomElement, "playerNames", out var playerNamesElement)
						&& playerNamesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
					{
						foreach (var playerName in playerNamesElement.EnumerateArray())
						{
							room.PlayerNames.Add(playerName.GetString() ?? "");
						}
					}
					rooms.Add(room);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\n[JSON ERROR] {ex.Message}");
				Thread.Sleep(3000);
				return;
			}

			// Listeyi göster
			int selectedIndex = _lobbyDisplay.NavigateRoomList(rooms);

			if (selectedIndex >= 0 && selectedIndex < rooms.Count)
			{
				JoinRoom(user, rooms[selectedIndex].Id);
			}
			else if (selectedIndex == -2)
			{
				BrowseRooms(user);
			}
		}

		// --- BU YARDIMCI METOTLARI EN ALTA EKLEMEYİ UNUTMA ---
		private string GetSafeString(System.Text.Json.JsonElement element, string key1, string key2, string def = "")
		{
			if (element.TryGetProperty(key1, out var val)) return val.GetString() ?? def;
			if (element.TryGetProperty(key2, out var val2)) return val2.GetString() ?? def;
			return def;
		}

		private int GetSafeInt(System.Text.Json.JsonElement element, string key1, string key2, int def = 0)
		{
			if (element.TryGetProperty(key1, out var val) && val.TryGetInt32(out int res)) return res;
			if (element.TryGetProperty(key2, out var val2) && val2.TryGetInt32(out int res2)) return res2;
			return def;
		}

		private bool TryGetPropertyCaseInsensitive(System.Text.Json.JsonElement elm, string key, out System.Text.Json.JsonElement val)
		{
			if (elm.TryGetProperty(key, out val)) return true; // tam eşleşme
			if (elm.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out val)) return true; // PascalCase
			if (elm.TryGetProperty(key.ToLower(), out val)) return true; // lowerCase
			val = default;
			return false;
		}
		private void CreateRoom(User user)
		{
			if (_signalRClient == null || !_signalRClient.IsConnected) return;

			var roomData = _lobbyDisplay.ShowCreateRoomForm(user.Username);

			if (roomData == null) return;

			_lobbyDisplay.ShowLoadingAnimation("Creating room", 1500);

			var createTask = _signalRClient.CreateRoomAsync(
				roomData.RoomName,
				roomData.PlayerName,
				roomData.Theme,
				roomData.MaxPlayers
			);

			createTask.Wait();

			var response = createTask.Result;

			if (response != null)
			{
				_lobbyDisplay.ShowSuccessMessage("Room created successfully!");
			}
			else
			{
				_lobbyDisplay.ShowErrorMessage("Failed to create room");
			}
		}

		private void JoinRoom(User user, string roomId)
		{
			if (_signalRClient == null || !_signalRClient.IsConnected) return;

			_lobbyDisplay.ShowLoadingAnimation("Joining room", 1500);

			var joinTask = _signalRClient.JoinRoomAsync(roomId, user.Username);
			joinTask.Wait();

			bool success = joinTask.Result;

			if (success)
			{
				_lobbyDisplay.ShowSuccessMessage("Joined room successfully!");
			}
			else
			{
				_lobbyDisplay.ShowErrorMessage("Failed to join room");
			}
		}

		private void QuickJoin(User user)
		{
			if (_signalRClient == null || !_signalRClient.IsConnected) return;

			_lobbyDisplay.ShowLoadingAnimation("Finding available room", 2000);

			var roomListTask = _signalRClient.GetRoomListAsync();
			roomListTask.Wait();

			var roomListResponse = roomListTask.Result;

			if (roomListResponse == null)
			{
				_lobbyDisplay.ShowErrorMessage("No rooms available");
				return;
			}

			var jsonElement = (System.Text.Json.JsonElement)roomListResponse;
			var roomsArray = jsonElement.GetProperty("Rooms");

			foreach (var roomElement in roomsArray.EnumerateArray())
			{
				var roomId = roomElement.GetProperty("Id").GetString() ?? "";
				var currentPlayers = roomElement.GetProperty("CurrentPlayers").GetInt32();
				var maxPlayers = roomElement.GetProperty("MaxPlayers").GetInt32();

				if (currentPlayers < maxPlayers)
				{
					JoinRoom(user, roomId);
					return;
				}
			}

			_lobbyDisplay.ShowErrorMessage("No available rooms found");
		}

		

		

	

		private void StartGame(User user, bool singlePlayer)
		{
			var preferences = _preferencesRepository.GetByUserId(user.Id);
			string theme = preferences?.Theme ?? "Desert";

			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      SELECT THEME                            ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
			Console.WriteLine("1. Desert Theme");
			Console.WriteLine("2. Forest Theme");
			Console.WriteLine("3. City Theme");
			Console.WriteLine($"4. Use Default ({theme})");
			Console.Write("\nSelect theme: ");

			string? themeChoice = Console.ReadLine();
			theme = themeChoice switch
			{
				"1" => "Desert",
				"2" => "Forest",
				"3" => "City",
				_ => theme
			};

			Console.WriteLine("\nStarting game...");
			Thread.Sleep(1000);

			var gameController = new GameController();
			gameController.StartGame(theme, singlePlayer);
		}

		private void ShowMyScores(User user)
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                       MY HIGH SCORES                         ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			var scores = _scoreRepository.GetUserScores(user.Id);
			int rank = 1;

			if (!scores.Any())
			{
				Console.WriteLine("No scores yet. Play a game to set your first score!");
			}
			else
			{
				foreach (var score in scores.Take(10))
				{
					Console.WriteLine($"{rank}. Score: {score.Score} | Date: {score.GameDate:yyyy-MM-dd HH:mm}");
					rank++;
				}
			}

			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
		}

		private void SettingsMenu(User user)
		{
			var preferences = _preferencesRepository.GetByUserId(user.Id);
			if (preferences == null)
			{
				preferences = new PlayerPreference
				{
					UserId = user.Id,
					Theme = "Desert",
					SoundEnabled = true
				};
				_preferencesRepository.Add(preferences);
			}

			while (true)
			{
				Console.Clear();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                         SETTINGS                             ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
				Console.WriteLine($"Current Theme: {preferences.Theme}");
				Console.WriteLine($"Sound: {(preferences.SoundEnabled ? "Enabled" : "Disabled")}\n");
				Console.WriteLine("1. Change Theme");
				Console.WriteLine("2. Toggle Sound");
				Console.WriteLine("3. Back");
				Console.Write("\nSelect option: ");

				string? choice = Console.ReadLine();

				switch (choice)
				{
					case "1":
						Console.WriteLine("\n1. Desert\n2. Forest\n3. City");
						Console.Write("Select theme: ");
						string? themeChoice = Console.ReadLine();
						preferences.Theme = themeChoice switch
						{
							"1" => "Desert",
							"2" => "Forest",
							"3" => "City",
							_ => preferences.Theme
						};
						_preferencesRepository.Update(preferences);
						Console.WriteLine("✓ Theme updated!");
						Thread.Sleep(1000);
						break;
					case "2":
						preferences.SoundEnabled = !preferences.SoundEnabled;
						_preferencesRepository.Update(preferences);
						Console.WriteLine($"✓ Sound {(preferences.SoundEnabled ? "enabled" : "disabled")}!");
						Thread.Sleep(1000);
						break;
					case "3":
						return;
				}
			}
		}

		private void ShowLeaderboard()
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                    GLOBAL LEADERBOARD                        ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			var topScores = _scoreRepository.GetTopScores(10);
			int rank = 1;

			if (!topScores.Any())
			{
				Console.WriteLine("No scores yet. Be the first to play!");
			}
			else
			{
				Console.WriteLine("Rank | Player            | Score    | Date");
				Console.WriteLine("─────┼───────────────────┼──────────┼────────────────");
				foreach (var score in topScores)
				{
					Console.WriteLine($"{rank,4} | {score.Username,-17} | {score.Score,8} | {score.GameDate:yyyy-MM-dd}");
					rank++;
				}
			}

			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
		}

		private string ReadPassword()
		{
			string password = "";
			ConsoleKeyInfo key;

			do
			{
				key = Console.ReadKey(true);

				if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
				{
					password += key.KeyChar;
					Console.Write("*");
				}
				else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
				{
					password = password.Substring(0, password.Length - 1);
					Console.Write("\b \b");
				}
			}
			while (key.Key != ConsoleKey.Enter);

			return password;
		}
	}
}