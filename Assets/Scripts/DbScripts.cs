using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

public class Test : MonoBehaviour
{
    private string dbName = "URI=file:GameData.db";

    void Start()
    {
        CreateDb();

        // Add sample users
        AddUser("Andrew", "1234");
        AddUser("John", "1234");

        // Add a sample game history
        AddGameHistory(1, 2, 3, 10, 8);

        // Display all users and game history
        DisplayUsers();
        DisplayGameHistory();
    }

    public void CreateDb()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // Create User table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS User (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name VARCHAR(50),
                        password VARCHAR(50),
                        wins INT
                    );";
                command.ExecuteNonQuery();

                // Create GameHistory table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS GameHistory (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        player1_id INT,
                        player2_id INT,
                        rounds INT,
                        player1_score INT,
                        player2_score INT,
                        FOREIGN KEY(player1_id) REFERENCES User(id),
                        FOREIGN KEY(player2_id) REFERENCES User(id)
                    );";
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    public void AddUser(string name, string password)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO User (name, password, wins) 
                    VALUES (@name, @password, @wins);";
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@wins", 0);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    public void AddGameHistory(int player1Id, int player2Id, int rounds, int player1Score, int player2Score)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO GameHistory (player1_id, player2_id, rounds, player1_score, player2_score) 
                    VALUES (@player1_id, @player2_id, @rounds, @player1_score, @player2_score);";
                command.Parameters.AddWithValue("@player1_id", player1Id);
                command.Parameters.AddWithValue("@player2_id", player2Id);
                command.Parameters.AddWithValue("@rounds", rounds);
                command.Parameters.AddWithValue("@player1_score", player1Score);
                command.Parameters.AddWithValue("@player2_score", player2Score);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    public void DisplayUsers()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM User;";

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Debug.Log($"ID: {reader["id"]}, Name: {reader["name"]}, Wins: {reader["wins"]}");
                    }
                }
            }

            connection.Close();
        }
    }

    public void DisplayGameHistory()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        g.id, 
                        u1.name AS player1_name, 
                        u2.name AS player2_name, 
                        g.rounds, 
                        g.player1_score, 
                        g.player2_score
                    FROM GameHistory g
                    JOIN User u1 ON g.player1_id = u1.id
                    JOIN User u2 ON g.player2_id = u2.id;";

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Debug.Log($"Game ID: {reader["id"]}, " +
                                  $"Player 1: {reader["player1_name"]}, Score: {reader["player1_score"]}, " +
                                  $"Player 2: {reader["player2_name"]}, Score: {reader["player2_score"]}, " +
                                  $"Rounds: {reader["rounds"]}");
                    }
                }
            }

            connection.Close();
        }
    }
}
