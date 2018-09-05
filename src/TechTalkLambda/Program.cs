#region imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using NLog;

#endregion

namespace TechTalkLambda {
  internal static class Program {

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // This is bad pracitce, IRL.
    private const string DbName = "db";

    public static void Main(string[] args) {
      Log.Info("Application started.");

      Log.Debug("Heros:");
      foreach (var hero in GetHeros()) {
        var heroId = Convert.ToInt32(hero["Id"]);

        Log.Debug($" -- {hero["Name"]} is {hero["Alias"]} ");

        Log.Debug($"    -- with powers");
        foreach (var power in GetPowers(heroId)) {
          Log.Debug($"       {power["Name"]}");
        }

        Log.Debug($"    -- frequents");
        foreach (var location in GetLocations(heroId)) {
          Log.Debug($"       {location["Name"]}");
        }
      }

      Log.Info("Application complete.");
    }

    private static void HandleException(Exception ex) {
      Log.Error($"EXCEPTION: {ex.GetType().Name} :: {ex.Message}");
      Log.Error(ex);
    }

    private static void CreateConnection(Action<SqlConnection> action, Action<Exception> except = null) {
      try {

        except = except ?? HandleException;
        
        using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[DbName].ConnectionString)) {
          conn.Open();
          action?.Invoke(conn);
        }
        
      } catch (Exception ex) {
        except?.Invoke(ex);
      }
    }

    private static void CreateCommand(Action<SqlCommand> action, Action<Exception> except = null) {
      CreateConnection(conn => {
        using (var cmd = new SqlCommand {Connection = conn}) {
          action?.Invoke(cmd);
        }
      },except);
    }

    private static void ExecuteQuery(string query, Action<SqlDataReader> action, Action<Exception> except = null) {
      CreateCommand(cmd => {
        cmd.CommandText = query;
        using (var reader = cmd.ExecuteReader()) {
          action?.Invoke(reader);
        }
      },except);
    }

    private static IEnumerable<Dictionary<string, object>> GetData(string query) {
      var data = new List<Dictionary<string, object>>();

      ExecuteQuery(query, reader => {
        
        while (reader.Read()) {
          var item = new Dictionary<string, object>();
          for (var i = 0; i < reader.FieldCount; i++) {
            item.Add(reader.GetName(i), reader[i]);
          }
          data.Add(item);
        }
      });

      return data;
    }

    private static IEnumerable<Dictionary<string, object>> GetHeros() {
      return GetData( "SELECT ID, NAME, ALIAS FROM HEROS");
    }

    private static IEnumerable<Dictionary<string, object>> GetLocations(int heroId) {
      return GetData($"SELECT ID, NAME FROM LOCATIONS WHERE HEROID = {heroId};");
    }

    private static IEnumerable<Dictionary<string, object>> GetPowers(int heroId) {
      return GetData($"SELECT ID, NAME FROM POWERS WHERE HEROID = {heroId};");
    }
  }
}