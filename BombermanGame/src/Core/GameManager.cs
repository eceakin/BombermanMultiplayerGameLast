using System;
using System.Collections.Generic;
using System.Linq;
using BombermanGame.src.Models;
using BombermanGame.src.Patterns.Behavioral.Observer;
using BombermanGame.src.Network;
using BombermanGame.src.Models.Network;

namespace BombermanGame.src.Core
{
	public sealed class GameManager : ISubject
	{
		private static GameManager? _instance;
		private static readonly object _lock = new object();
		private List<IObserver> _observers = new List<IObserver>();

		public Map? CurrentMap { get; set; }
		public List<Player> Players { get; set; } = new List<Player>();
		public List<Bomb> Bombs { get; set; } = new List<Bomb>();
		public List<Enemy> Enemies { get; set; } = new List<Enemy>();
		public List<PowerUp> PowerUps { get; set; } = new List<PowerUp>();
		public bool IsGameRunning { get; set; }
		public int CurrentUserId { get; set; }

		public bool IsOnlineMode { get; set; }
		public string CurrentRoomId { get; set; } = "";
		public string MyConnectionId { get; set; } = "";
		public SignalRClient? NetworkClient { get; set; }

		private GameManager() { }

		public static GameManager Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (_lock)
					{
						if (_instance == null)
						{
							_instance = new GameManager();
						}
					}
				}
				return _instance;
			}
		}

		public void Attach(IObserver observer)
		{
			if (!_observers.Contains(observer))
			{
				_observers.Add(observer);
			}
		}

		public void Detach(IObserver observer)
		{
			_observers.Remove(observer);
		}

		public void Notify(GameEvent gameEvent)
		{
			foreach (var observer in _observers)
			{
				observer.Update(gameEvent);
			}
		}

		public void ResetGame()
		{
			Players.Clear();
			Bombs.Clear();
			Enemies.Clear();
			PowerUps.Clear();
			IsGameRunning = false;
			IsOnlineMode = false;
			CurrentRoomId = "";
		}

		public void SyncFromServer(GameStateSync gameState)
		{
			if (!IsOnlineMode) return;

			lock (_lock)
			{
				SyncPlayers(gameState.Players);
				SyncBombs(gameState.Bombs);
				SyncEnemies(gameState.Enemies);
				SyncPowerUps(gameState.PowerUps);
			}
		}

		public GameStateSync GetSyncData()
		{
			lock (_lock)
			{
				return new GameStateSync
				{
					Players = Players.Select(p => new Models.Network
					.PlayerInfo
					{
						ConnectionId = p.Id == GetLocalPlayerId() ? MyConnectionId : "",
						Name = p.Name,
						PlayerId = p.Id,
						X = p.Position.X,
						Y = p.Position.Y,
						IsAlive = p.IsAlive,
						BombCount = p.BombCount,
						BombPower = p.BombPower,
						Speed = p.Speed,
						Score = p.Score,
						Health = p.Health
					}).ToList(),
					Bombs = Bombs.Where(b => !b.HasExploded).Select(b => new BombInfo
					{
						Id = Guid.NewGuid().ToString(),
						OwnerId = b.OwnerId.ToString(),
						X = b.Position.X,
						Y = b.Position.Y,
						Timer = b.Timer,
						Range = b.Power,
						HasExploded = b.HasExploded
					}).ToList(),
					Enemies = Enemies.Where(e => e.IsAlive).Select(e => new EnemyInfo
					{
						Id = e.Id.ToString(),
						Type = e.Type.ToString() + "Enemy",
						X = e.Position.X,
						Y = e.Position.Y,
						IsAlive = e.IsAlive,
						Health = e.Health
					}).ToList(),
					PowerUps = PowerUps.Where(p => !p.IsCollected).Select(p => new PowerUpInfo
					{
						Id = Guid.NewGuid().ToString(),
						Type = p.Type.ToString(),
						X = p.Position.X,
						Y = p.Position.Y,
						IsCollected = p.IsCollected
					}).ToList(),
					GameTick = Environment.TickCount,
					Timestamp = DateTime.UtcNow
				};
			}
		}

		private void SyncPlayers(List<Models.Network.PlayerInfo> playerInfos)
		{
			foreach (var playerInfo in playerInfos)
			{
				var player = Players.FirstOrDefault(p => p.Id == playerInfo.PlayerId);
				if (player != null && player.Id != GetLocalPlayerId())
				{
					player.Position.X = playerInfo.X;
					player.Position.Y = playerInfo.Y;
					player.IsAlive = playerInfo.IsAlive;
					player.Score = playerInfo.Score;
					player.Health = playerInfo.Health;
				}
			}
		}

		private void SyncBombs(List<BombInfo> bombInfos)
		{
			var localPlayerId = GetLocalPlayerId();
			var remoteBombs = Bombs.Where(b => b.OwnerId != localPlayerId).ToList();

			foreach (var remoteBomb in remoteBombs)
			{
				Bombs.Remove(remoteBomb);
			}

			foreach (var bombInfo in bombInfos)
			{
				int ownerId = int.Parse(bombInfo.OwnerId);
				if (ownerId != localPlayerId && !bombInfo.HasExploded)
				{
					var existingBomb = Bombs.FirstOrDefault(b =>
						b.Position.X == bombInfo.X &&
						b.Position.Y == bombInfo.Y &&
						b.OwnerId == ownerId);

					if (existingBomb == null)
					{
						var bomb = new Bomb(
							new Position(bombInfo.X, bombInfo.Y),
							bombInfo.Range,
							ownerId
						)
						{
							Timer = bombInfo.Timer,
							HasExploded = bombInfo.HasExploded
						};
						Bombs.Add(bomb);
					}
					else
					{
						existingBomb.Timer = bombInfo.Timer;
					}
				}
			}
		}

		private void SyncEnemies(List<EnemyInfo> enemyInfos)
		{
			Enemies.Clear();
			foreach (var enemyInfo in enemyInfos)
			{
				if (enemyInfo.IsAlive)
				{
					var enemyType = Enum.Parse<EnemyType>(enemyInfo.Type.Replace("Enemy", ""));
					var enemy = new Enemy(
						int.Parse(enemyInfo.Id),
						new Position(enemyInfo.X, enemyInfo.Y),
						enemyType
					)
					{
						IsAlive = enemyInfo.IsAlive,
						Health = enemyInfo.Health
					};
					Enemies.Add(enemy);
				}
			}
		}

		private void SyncPowerUps(List<PowerUpInfo> powerUpInfos)
		{
			PowerUps.Clear();
			foreach (var powerUpInfo in powerUpInfos)
			{
				if (!powerUpInfo.IsCollected)
				{
					var powerUpType = Enum.Parse<PowerUpType>(powerUpInfo.Type);
					var powerUp = new PowerUp(
						new Position(powerUpInfo.X, powerUpInfo.Y),
						powerUpType
					)
					{
						IsCollected = powerUpInfo.IsCollected
					};
					PowerUps.Add(powerUp);
				}
			}
		}

		private int GetLocalPlayerId()
		{
			if (!IsOnlineMode || Players.Count == 0) return 1;

			var localPlayer = Players.FirstOrDefault(p =>
				p.Id.ToString() == MyConnectionId ||
				(Players.Count > 0 && p.Id == 1));

			return localPlayer?.Id ?? 1;
		}

		public Player? GetLocalPlayer()
		{
			return Players.FirstOrDefault(p => p.Id == GetLocalPlayerId());
		}

		public List<Player> GetRemotePlayers()
		{
			var localPlayerId = GetLocalPlayerId();
			return Players.Where(p => p.Id != localPlayerId).ToList();
		}

		public void EnableOnlineMode(SignalRClient client, string roomId, string connectionId)
		{
			IsOnlineMode = true;
			NetworkClient = client;
			CurrentRoomId = roomId;
			MyConnectionId = connectionId;
		}

		public void DisableOnlineMode()
		{
			IsOnlineMode = false;
			NetworkClient = null;
			CurrentRoomId = "";
			MyConnectionId = "";
		}
	}
}