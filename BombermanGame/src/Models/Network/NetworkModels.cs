// Models/Network/NetworkModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using BombermanGame.src.Models;
using BombermanGame.src.Core;

namespace BombermanGame.src.Models.Network
{
	#region DTOs

	public class RoomInfo
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
		public string Theme { get; set; } = "Desert";
		public string State { get; set; } = "Waiting";
		public int CurrentPlayers { get; set; }
		public int MaxPlayers { get; set; } = 2;
		public List<string> PlayerNames { get; set; } = new();
		public int MapSeed { get; set; }
		public bool IsFull => CurrentPlayers >= MaxPlayers;
		public bool CanJoin => State == "Waiting" && !IsFull;
	}

	public class PlayerInfo
	{
		public string ConnectionId { get; set; } = "";
		public string Name { get; set; } = "";
		public int PlayerId { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public bool IsAlive { get; set; }
		public int BombCount { get; set; }
		public int BombPower { get; set; }
		public int Speed { get; set; }
		public int Score { get; set; }
		public int Health { get; set; }
	}

	public class GameStateSync
	{
		public List<PlayerInfo> Players { get; set; } = new();
		public List<BombInfo> Bombs { get; set; } = new();
		public List<EnemyInfo> Enemies { get; set; } = new();
		public List<PowerUpInfo> PowerUps { get; set; } = new();
		public int GameTick { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}

	public class BombInfo
	{
		public string Id { get; set; } = "";
		public string OwnerId { get; set; } = "";
		public int X { get; set; }
		public int Y { get; set; }
		public int Timer { get; set; }
		public int Range { get; set; }
		public bool HasExploded { get; set; }
	}

	public class EnemyInfo
	{
		public string Id { get; set; } = "";
		public string Type { get; set; } = "";
		public int X { get; set; }
		public int Y { get; set; }
		public bool IsAlive { get; set; }
		public int Health { get; set; }
	}

	public class PowerUpInfo
	{
		public string Id { get; set; } = "";
		public string Type { get; set; } = "";
		public int X { get; set; }
		public int Y { get; set; }
		public bool IsCollected { get; set; }
	}

	public class MoveMessage
	{
		public string RoomId { get; set; } = "";
		public string PlayerId { get; set; } = "";
		public int X { get; set; }
		public int Y { get; set; }
		public long Timestamp { get; set; }
	}

	public class BombMessage
	{
		public string RoomId { get; set; } = "";
		public string PlayerId { get; set; } = "";
		public int X { get; set; }
		public int Y { get; set; }
		public int Range { get; set; }
		public long Timestamp { get; set; }
	}

	#endregion

	#region Mapping Extensions

	public static class NetworkMappingExtensions
	{
		public static PlayerInfo ToPlayerInfo(this Player player, string connectionId = "")
		{
			return new PlayerInfo
			{
				ConnectionId = connectionId,
				Name = player.Name,
				PlayerId = player.Id,
				X = player.Position.X,
				Y = player.Position.Y,
				IsAlive = player.IsAlive,
				BombCount = player.BombCount,
				BombPower = player.BombPower,
				Speed = player.Speed,
				Score = player.Score,
				Health = player.Health
			};
		}

		public static BombInfo ToBombInfo(this Bomb bomb)
		{
			return new BombInfo
			{
				Id = Guid.NewGuid().ToString(),
				OwnerId = bomb.OwnerId.ToString(),
				X = bomb.Position.X,
				Y = bomb.Position.Y,
				Timer = bomb.Timer,
				Range = bomb.Power,
				HasExploded = bomb.HasExploded
			};
		}

		public static EnemyInfo ToEnemyInfo(this Enemy enemy)
		{
			return new EnemyInfo
			{
				Id = enemy.Id.ToString(),
				Type = enemy.Type.ToString() + "Enemy",
				X = enemy.Position.X,
				Y = enemy.Position.Y,
				IsAlive = enemy.IsAlive,
				Health = enemy.Health
			};
		}

		public static PowerUpInfo ToPowerUpInfo(this PowerUp powerUp)
		{
			return new PowerUpInfo
			{
				Id = Guid.NewGuid().ToString(),
				Type = powerUp.Type.ToString(),
				X = powerUp.Position.X,
				Y = powerUp.Position.Y,
				IsCollected = powerUp.IsCollected
			};
		}

		public static GameStateSync ToGameState(this GameManager gameManager)
		{
			return new GameStateSync
			{
				Players = gameManager.Players.Select(p => p.ToPlayerInfo()).ToList(),
				Bombs = gameManager.Bombs.Where(b => !b.HasExploded).Select(b => b.ToBombInfo()).ToList(),
				Enemies = gameManager.Enemies.Where(e => e.IsAlive).Select(e => e.ToEnemyInfo()).ToList(),
				PowerUps = gameManager.PowerUps.Where(p => !p.IsCollected).Select(p => p.ToPowerUpInfo()).ToList(),
				GameTick = Environment.TickCount,
				Timestamp = DateTime.UtcNow
			};
		}

		public static void FromServerDTO(this GameManager gameManager, GameStateSync state)
		{
			foreach (var playerInfo in state.Players)
			{
				var player = gameManager.Players.FirstOrDefault(p => p.Id == playerInfo.PlayerId);
				if (player != null)
				{
					player.Position.X = playerInfo.X;
					player.Position.Y = playerInfo.Y;
					player.IsAlive = playerInfo.IsAlive;
					player.Score = playerInfo.Score;
					player.Health = playerInfo.Health;
				}
			}

			gameManager.Bombs.Clear();
			foreach (var bombInfo in state.Bombs)
			{
				if (!bombInfo.HasExploded)
				{
					var bomb = new Bomb(
						new Position(bombInfo.X, bombInfo.Y),
						bombInfo.Range,
						int.Parse(bombInfo.OwnerId)
					)
					{
						Timer = bombInfo.Timer,
						HasExploded = bombInfo.HasExploded
					};
					gameManager.Bombs.Add(bomb);
				}
			}

			gameManager.Enemies.Clear();
			foreach (var enemyInfo in state.Enemies)
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
					gameManager.Enemies.Add(enemy);
				}
			}

			gameManager.PowerUps.Clear();
			foreach (var powerUpInfo in state.PowerUps)
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
					gameManager.PowerUps.Add(powerUp);
				}
			}
		}

		public static MoveMessage ToMoveMessage(this Player player, string roomId)
		{
			return new MoveMessage
			{
				RoomId = roomId,
				PlayerId = player.Id.ToString(),
				X = player.Position.X,
				Y = player.Position.Y,
				Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			};
		}

		public static BombMessage ToBombMessage(this Bomb bomb, string roomId)
		{
			return new BombMessage
			{
				RoomId = roomId,
				PlayerId = bomb.OwnerId.ToString(),
				X = bomb.Position.X,
				Y = bomb.Position.Y,
				Range = bomb.Power,
				Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			};
		}
	}

	#endregion
}