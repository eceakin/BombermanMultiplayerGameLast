using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BombermanGame.src.UI
{
	/// <summary>
	/// Online multiplayer lobby UI - Oda listesi, oluşturma ve bekleme ekranları
	/// AŞAMA 2.1: Lobby Display Implementation
	/// </summary>
	public class LobbyDisplay
	{
		private const int REFRESH_INTERVAL = 2000; // 2 saniye
		private bool _isWaiting = false;
		private CancellationTokenSource? _refreshCancellation;

		#region Main Lobby Menu

		/// <summary>
		/// Ana lobby menüsünü göster
		/// </summary>
		public void DisplayLobbyMenu(string username, int onlineCount = 0)
		{
			Console.Clear();
			DrawLobbyHeader(username, onlineCount);

			Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      ONLINE LOBBY                            ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			Console.WriteLine("1. Create Room");
			Console.WriteLine("2. Browse Rooms");
			Console.WriteLine("3. Quick Join (Random Room)");
			Console.WriteLine("4. Refresh");
			Console.WriteLine("5. Back to Main Menu");
			Console.WriteLine("\n──────────────────────────────────────────────────────────────");
		}

		/// <summary>
		/// Lobby başlığını çiz
		/// </summary>
		private void DrawLobbyHeader(string username, int onlineCount)
		{
			ConsoleUI.WriteLineColored("╔══════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
			ConsoleUI.WriteLineColored($"║  👤 Player: {username,-47} ║", ConsoleColor.Cyan);
			ConsoleUI.WriteLineColored($"║  🌐 Online: {onlineCount} players{new string(' ', 41)} ║", ConsoleColor.Cyan);
			ConsoleUI.WriteLineColored("╚══════════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);
		}

		/// <summary>
		/// Lobby menüsünde gezinme (klavye navigasyonu)
		/// </summary>
		public int NavigateLobbyMenu(string username, int onlineCount = 0, int currentIndex = 0)
		{
			var options = new List<string>
			{
				"Create Room",
				"Browse Rooms",
				"Quick Join (Random Room)",
				"Refresh",
				"Back to Main Menu"
			};

			while (true)
			{
				Console.Clear();
				DrawLobbyHeader(username, onlineCount);

				Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                      ONLINE LOBBY                            ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

				for (int i = 0; i < options.Count; i++)
				{
					if (i == currentIndex)
					{
						ConsoleUI.WriteColored($"► {i + 1}. {options[i]}\n", ConsoleColor.Yellow);
					}
					else
					{
						Console.WriteLine($"  {i + 1}. {options[i]}");
					}
				}

				Console.WriteLine("\n──────────────────────────────────────────────────────────────");
				Console.WriteLine("Use ↑↓ or W/S to navigate | Enter to select | ESC to go back");

				var key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.UpArrow:
					case ConsoleKey.W:
						currentIndex = (currentIndex - 1 + options.Count) % options.Count;
						break;

					case ConsoleKey.DownArrow:
					case ConsoleKey.S:
						currentIndex = (currentIndex + 1) % options.Count;
						break;

					case ConsoleKey.Enter:
					case ConsoleKey.Spacebar:
						return currentIndex + 1; // Return 1-based index

					case ConsoleKey.Escape:
						return 5; // Back to main menu

					case ConsoleKey.D1:
					case ConsoleKey.NumPad1:
						return 1;
					case ConsoleKey.D2:
					case ConsoleKey.NumPad2:
						return 2;
					case ConsoleKey.D3:
					case ConsoleKey.NumPad3:
						return 3;
					case ConsoleKey.D4:
					case ConsoleKey.NumPad4:
						return 4;
					case ConsoleKey.D5:
					case ConsoleKey.NumPad5:
						return 5;
				}
			}
		}

		#endregion

		#region Room List Display

		/// <summary>
		/// Oda listesini göster
		/// </summary>
		public void ShowRoomList(List<RoomInfo> rooms, int selectedIndex = -1)
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      AVAILABLE ROOMS                         ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			if (rooms == null || rooms.Count == 0)
			{
				ConsoleUI.WriteLineColored("  No rooms available. Create one to get started!\n", ConsoleColor.DarkGray);
				Console.WriteLine("\nPress any key to go back...");
				return;
			}

			Console.WriteLine("ID   | Room Name          | Players | Theme    | Status");
			Console.WriteLine("─────┼────────────────────┼─────────┼──────────┼─────────");

			for (int i = 0; i < rooms.Count; i++)
			{
				var room = rooms[i];
				var statusColor = room.State == "Waiting" ? ConsoleColor.Green : ConsoleColor.Yellow;
				var isSelected = i == selectedIndex;

				if (isSelected)
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("► ");
				}
				else
				{
					Console.Write("  ");
				}

				// Room number
				Console.Write($"{i + 1,-3} | ");

				// Room name
				Console.Write($"{TruncateString(room.Name, 18),-18} | ");

				// Players
				var playerStr = $"{room.CurrentPlayers}/{room.MaxPlayers}";
				if (room.IsFull)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Write($"{playerStr,-7} | ");
					Console.ResetColor();
				}
				else
				{
					Console.Write($"{playerStr,-7} | ");
				}

				// Theme
				Console.Write($"{room.Theme,-8} | ");

				// Status
				Console.ForegroundColor = statusColor;
				Console.Write(room.State);
				Console.ResetColor();
				Console.WriteLine();

				if (isSelected)
				{
					Console.ResetColor();
				}
			}

			Console.WriteLine("─────┴────────────────────┴─────────┴──────────┴─────────");
			Console.WriteLine("\nUse ↑↓ to navigate | Enter to join | ESC to go back | R to refresh");
		}

		/// <summary>
		/// Oda listesinde gezinme
		/// </summary>
		public int NavigateRoomList(List<RoomInfo> rooms)
		{
			if (rooms == null || rooms.Count == 0)
			{
				ShowRoomList(rooms);
				Console.ReadKey(true);
				return -1;
			}

			int selectedIndex = 0;

			while (true)
			{
				ShowRoomList(rooms, selectedIndex);

				var key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.UpArrow:
					case ConsoleKey.W:
						selectedIndex = (selectedIndex - 1 + rooms.Count) % rooms.Count;
						break;

					case ConsoleKey.DownArrow:
					case ConsoleKey.S:
						selectedIndex = (selectedIndex + 1) % rooms.Count;
						break;

					case ConsoleKey.Enter:
					case ConsoleKey.Spacebar:
						if (!rooms[selectedIndex].IsFull)
						{
							return selectedIndex; // Return selected room index
						}
						else
						{
							ConsoleUI.ShowError("Room is full!");
							Thread.Sleep(1500);
						}
						break;

					case ConsoleKey.R:
						return -2; // Signal to refresh

					case ConsoleKey.Escape:
						return -1; // Go back
				}
			}
		}

		#endregion

		#region Room Details

		/// <summary>
		/// Oda detaylarını göster
		/// </summary>
		public void ShowRoomDetails(RoomInfo room)
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      ROOM DETAILS                            ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			Console.WriteLine($"Room Name:     {room.Name}");
			Console.WriteLine($"Room ID:       {room.Id.Substring(0, 8)}...");
			Console.WriteLine($"Theme:         {room.Theme}");
			Console.WriteLine($"Status:        {room.State}");
			Console.WriteLine($"Players:       {room.CurrentPlayers}/{room.MaxPlayers}");
			Console.WriteLine();

			if (room.PlayerNames.Count > 0)
			{
				Console.WriteLine("Current Players:");
				Console.WriteLine("─────────────────────");
				for (int i = 0; i < room.PlayerNames.Count; i++)
				{
					var icon = i == 0 ? "👑" : "👤";
					Console.WriteLine($"{icon} {room.PlayerNames[i]}");
				}
			}

			Console.WriteLine("\n──────────────────────────────────────────────────────────────");
		}

		#endregion

		#region Create Room Form

		/// <summary>
		/// Oda oluşturma formu göster
		/// </summary>
		public CreateRoomData? ShowCreateRoomForm(string defaultPlayerName)
		{
			Console.Clear();
			Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      CREATE ROOM                             ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			// Room Name
			Console.Write("Room Name: ");
			string? roomName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(roomName))
			{
				roomName = $"{defaultPlayerName}'s Room";
			}

			// Player Name
			Console.Write($"Your Name (default: {defaultPlayerName}): ");
			string? playerName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = defaultPlayerName;
			}

			// Theme Selection
			Console.WriteLine("\nSelect Theme:");
			Console.WriteLine("  1. Desert");
			Console.WriteLine("  2. Forest");
			Console.WriteLine("  3. City");
			Console.Write("\nChoice (1-3, default: Desert): ");

			string? themeChoice = Console.ReadLine();
			string theme = themeChoice switch
			{
				"2" => "Forest",
				"3" => "City",
				_ => "Desert"
			};

			// Max Players
			Console.Write("\nMax Players (2-4, default: 2): ");
			string? maxPlayersInput = Console.ReadLine();
			int maxPlayers = 2;
			if (int.TryParse(maxPlayersInput, out int parsedMax))
			{
				maxPlayers = Math.Clamp(parsedMax, 2, 4);
			}

			// Confirmation
			Console.WriteLine("\n──────────────────────────────────────────────────────────────");
			Console.WriteLine("Room Configuration:");
			Console.WriteLine($"  Room Name:    {roomName}");
			Console.WriteLine($"  Your Name:    {playerName}");
			Console.WriteLine($"  Theme:        {theme}");
			Console.WriteLine($"  Max Players:  {maxPlayers}");
			Console.WriteLine("──────────────────────────────────────────────────────────────");

			Console.Write("\nCreate this room? (Y/N): ");
			var confirm = Console.ReadKey(true);

			if (confirm.Key != ConsoleKey.Y)
			{
				ConsoleUI.ShowWarning("Room creation cancelled");
				Thread.Sleep(1000);
				return null;
			}

			return new CreateRoomData
			{
				RoomName = roomName,
				PlayerName = playerName,
				Theme = theme,
				MaxPlayers = maxPlayers
			};
		}

		#endregion

		#region Player List

		/// <summary>
		/// Oyuncu listesini göster (odada beklerken)
		/// </summary>
		public void ShowPlayerList(List<string> playerNames, bool isHost)
		{
			Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
			Console.WriteLine("║                      PLAYERS IN ROOM                         ║");
			Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

			if (playerNames.Count == 0)
			{
				ConsoleUI.WriteLineColored("  No players in room\n", ConsoleColor.DarkGray);
				return;
			}

			for (int i = 0; i < playerNames.Count; i++)
			{
				var icon = i == 0 ? "👑" : "👤";
				var status = i == 0 ? "(Host)" : "";

				if (i == 0)
				{
					ConsoleUI.WriteColored($"{icon} {playerNames[i]} {status}\n", ConsoleColor.Yellow);
				}
				else
				{
					Console.WriteLine($"{icon} {playerNames[i]} {status}");
				}
			}

			Console.WriteLine();
		}

		#endregion

		#region Waiting Screen

		/// <summary>
		/// Bekleme ekranı - Oyun başlayana kadar
		/// </summary>
		public void ShowWaitingScreen(RoomInfo room, bool isHost, CancellationToken cancellationToken = default)
		{
			_isWaiting = true;
			int frame = 0;
			string[] spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

			while (_isWaiting && !cancellationToken.IsCancellationRequested)
			{
				Console.Clear();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                      WAITING FOR GAME                        ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

				// Room Info
				Console.WriteLine($"Room:    {room.Name}");
				Console.WriteLine($"Theme:   {room.Theme}");
				Console.WriteLine($"Players: {room.CurrentPlayers}/{room.MaxPlayers}");
				Console.WriteLine();

				// Player List
				ShowPlayerList(room.PlayerNames, isHost);

				// Waiting animation
				Console.Write($"\n{spinner[frame % spinner.Length]} ");

				if (isHost)
				{
					if (room.CurrentPlayers >= 2)
					{
						ConsoleUI.WriteColored("Ready to start! Press SPACE to begin", ConsoleColor.Green);
					}
					else
					{
						ConsoleUI.WriteColored("Waiting for at least 1 more player...", ConsoleColor.Yellow);
					}
					Console.WriteLine("\n\nPress ESC to cancel | SPACE to start (host only)");
				}
				else
				{
					ConsoleUI.WriteColored("Waiting for host to start the game...", ConsoleColor.Cyan);
					Console.WriteLine("\n\nPress ESC to leave room");
				}

				frame++;
				Thread.Sleep(200);

				// Check for input without blocking
				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(true);

					if (key.Key == ConsoleKey.Escape)
					{
						_isWaiting = false;
						return;
					}

					if (isHost && key.Key == ConsoleKey.Spacebar && room.CurrentPlayers >= 2)
					{
						_isWaiting = false;
						return;
					}
				}
			}
		}

		/// <summary>
		/// Bekleme ekranını durdur
		/// </summary>
		public void StopWaiting()
		{
			_isWaiting = false;
		}

		#endregion

		#region Game Starting Screen

		/// <summary>
		/// Oyun başlama countdown'u
		/// </summary>
		public void ShowGameStarting(int seconds = 3)
		{
			Console.Clear();

			for (int i = seconds; i > 0; i--)
			{
				Console.Clear();
				ConsoleUI.AddSpacing(10);

				var color = i switch
				{
					3 => ConsoleColor.Green,
					2 => ConsoleColor.Yellow,
					1 => ConsoleColor.Red,
					_ => ConsoleColor.White
				};

				int cursorLeft = Console.WindowWidth / 2 - 10;
				int cursorTop = Console.WindowHeight / 2;

				Console.SetCursorPosition(cursorLeft, cursorTop);
				ConsoleUI.WriteColored($"    GAME STARTING IN    ", ConsoleColor.White);

				Console.SetCursorPosition(cursorLeft + 5, cursorTop + 2);
				ConsoleUI.WriteColored($"        {i}        ", color);

				Thread.Sleep(1000);
			}

			Console.Clear();
			ConsoleUI.AddSpacing(10);
			Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2);
			ConsoleUI.WriteLineColored("         GO!         ", ConsoleColor.Cyan);
			Thread.Sleep(500);
		}

		#endregion

		#region Connection Status

		/// <summary>
		/// Bağlantı durumu göster
		/// </summary>
		public void ShowConnectionStatus(string status, bool isConnected)
		{
			Console.SetCursorPosition(0, Console.WindowHeight - 1);

			var color = isConnected ? ConsoleColor.Green : ConsoleColor.Red;
			var icon = isConnected ? "●" : "○";

			Console.Write("Connection: ");
			ConsoleUI.WriteColored($"{icon} {status}", color);
			Console.Write(new string(' ', 40)); // Clear rest of line
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// String'i belirtilen uzunlukta kırp
		/// </summary>
		private string TruncateString(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text))
				return new string(' ', maxLength);

			if (text.Length <= maxLength)
				return text;

			return text.Substring(0, maxLength - 3) + "...";
		}

		/// <summary>
		/// Yükleniyor animasyonu göster
		/// </summary>
		public void ShowLoadingAnimation(string message, int durationMs = 2000)
		{
			string[] frames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
			int iterations = durationMs / 100;

			for (int i = 0; i < iterations; i++)
			{
				Console.Write($"\r{frames[i % frames.Length]} {message}");
				Thread.Sleep(100);
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Başarı mesajı göster
		/// </summary>
		public void ShowSuccessMessage(string message)
		{
			Console.WriteLine();
			ConsoleUI.WriteLineColored($"✓ {message}", ConsoleColor.Green);
			Thread.Sleep(1500);
		}

		/// <summary>
		/// Hata mesajı göster
		/// </summary>
		public void ShowErrorMessage(string message)
		{
			Console.WriteLine();
			ConsoleUI.WriteLineColored($"✗ {message}", ConsoleColor.Red);
			Thread.Sleep(2000);
		}

		#endregion
	}

	#region Data Classes

	/// <summary>
	/// Oda oluşturma verileri
	/// </summary>
	public class CreateRoomData
	{
		public string RoomName { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public string Theme { get; set; } = "Desert";
		public int MaxPlayers { get; set; } = 2;
	}

	/// <summary>
	/// Oda bilgileri (DTO)
	/// </summary>
	public class RoomInfo
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
		public string Theme { get; set; } = "Desert";
		public string State { get; set; } = "Waiting";
		public int CurrentPlayers { get; set; }
		public int MaxPlayers { get; set; } = 2;
		public List<string> PlayerNames { get; set; } = new();
		public bool IsFull => CurrentPlayers >= MaxPlayers;
	}

	#endregion
}