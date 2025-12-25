// MVC/Controllers/InputController.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BombermanGame.src.Patterns.Behavioral.Command;
using BombermanGame.src.Models;
using BombermanGame.src.Network;

namespace BombermanGame.src.MVC.Controllers
{
	public class InputController
	{
		private Dictionary<int, PlayerControls> _playerControls;
		private CommandInvoker _commandInvoker;
		private SignalRClient? _signalRClient;
		private string _currentRoomId = "";

		public bool IsOnlineMode { get; set; }

		public InputController()
		{
			_commandInvoker = new CommandInvoker();
			_playerControls = new Dictionary<int, PlayerControls>();
			IsOnlineMode = false;

			_playerControls[1] = new PlayerControls
			{
				Up = ConsoleKey.W,
				Down = ConsoleKey.S,
				Left = ConsoleKey.A,
				Right = ConsoleKey.D,
				PlaceBomb = ConsoleKey.Spacebar,
				AlternateUp = ConsoleKey.UpArrow,
				AlternateDown = ConsoleKey.DownArrow,
				AlternateLeft = ConsoleKey.LeftArrow,
				AlternateRight = ConsoleKey.RightArrow
			};

			_playerControls[2] = new PlayerControls
			{
				Up = ConsoleKey.I,
				Down = ConsoleKey.K,
				Left = ConsoleKey.J,
				Right = ConsoleKey.L,
				PlaceBomb = ConsoleKey.Enter
			};
		}

		public void SetOnlineMode(SignalRClient client, string roomId)
		{
			IsOnlineMode = true;
			_signalRClient = client;
			_currentRoomId = roomId;
		}

		public void SetOfflineMode()
		{
			IsOnlineMode = false;
			_signalRClient = null;
			_currentRoomId = "";
		}

		public ICommand? ProcessInput(ConsoleKeyInfo key, Player player, Map map)
		{
			if (!_playerControls.ContainsKey(player.Id))
				return null;

			var controls = _playerControls[player.Id];

			if (key.Key == controls.Up || key.Key == controls.AlternateUp)
				return new MoveCommand(player, 0, -1, map);
			else if (key.Key == controls.Down || key.Key == controls.AlternateDown)
				return new MoveCommand(player, 0, 1, map);
			else if (key.Key == controls.Left || key.Key == controls.AlternateLeft)
				return new MoveCommand(player, -1, 0, map);
			else if (key.Key == controls.Right || key.Key == controls.AlternateRight)
				return new MoveCommand(player, 1, 0, map);
			else if (key.Key == controls.PlaceBomb)
				return new PlaceBombCommand(player);

			return null;
		}

		public async Task ProcessOnlineInput(ConsoleKeyInfo key, Player player, Map map)
		{
			if (!IsOnlineMode || _signalRClient == null || !player.IsAlive)
				return;

			var oldX = player.Position.X;
			var oldY = player.Position.Y;

			var command = ProcessInput(key, player, map);
			if (command != null)
			{
				_commandInvoker.ExecuteCommand(command);

				if (player.Position.X != oldX || player.Position.Y != oldY)
				{
					await _signalRClient.SendMoveAsync(_currentRoomId, player.Position.X, player.Position.Y);
				}
				else if (command is PlaceBombCommand)
				{
					var gameManager = Core.GameManager.Instance;
					var bomb = gameManager.Bombs.LastOrDefault(b => b.OwnerId == player.Id);
					if (bomb != null)
					{
						await _signalRClient.SendPlaceBombAsync(_currentRoomId, bomb.Position.X, bomb.Position.Y, bomb.Power);
					}
				}
			}
		}

		public void ProcessMultiplayerInput(ConsoleKeyInfo key, List<Player> players, Map map)
		{
			foreach (var player in players)
			{
				if (!player.IsAlive) continue;

				var command = ProcessInput(key, player, map);
				if (command != null)
				{
					_commandInvoker.ExecuteCommand(command);
					break;
				}
			}
		}

		public void UndoLastCommand()
		{
			_commandInvoker.UndoLastCommand();
		}

		public void ClearHistory()
		{
			_commandInvoker.ClearHistory();
		}

		public void SetPlayerControls(int playerId, PlayerControls controls)
		{
			_playerControls[playerId] = controls;
		}

		public PlayerControls? GetPlayerControls(int playerId)
		{
			return _playerControls.ContainsKey(playerId) ? _playerControls[playerId] : null;
		}
	}

	public class PlayerControls
	{
		public ConsoleKey Up { get; set; }
		public ConsoleKey Down { get; set; }
		public ConsoleKey Left { get; set; }
		public ConsoleKey Right { get; set; }
		public ConsoleKey PlaceBomb { get; set; }
		public ConsoleKey? AlternateUp { get; set; }
		public ConsoleKey? AlternateDown { get; set; }
		public ConsoleKey? AlternateLeft { get; set; }
		public ConsoleKey? AlternateRight { get; set; }

		public string GetControlsString()
		{
			return $"↑:{Up} ↓:{Down} ←:{Left} →:{Right} Bomb:{PlaceBomb}";
		}
	}
}