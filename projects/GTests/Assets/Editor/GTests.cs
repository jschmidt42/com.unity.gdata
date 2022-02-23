using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class GTests
{
    static readonly GJSONFormatting DF = GJSONFormatting.EscapeBraces;

    [SetUp]
    public void Setup()
    {
        G.Reset();
    }

    [TearDown]
    public void LogResults()
    {
        #if UNITY_EDITOR
        var json = G.ToJSON();
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, json.Replace("{", "{{").Replace("}", "}}"));
        System.IO.File.WriteAllText("Temp/g.json", json);
        UnityEditor.Search.SearchService.RefreshWindows();
        #endif
    }

    [Test]
    public void CreateEntities()
    {
        var game1 = G.Create("Game");
        var game2 = G.Create("Game");
        var turn = G.Create(game1, "Turn");
        var players = G.Create(game1, "Players");

        Debug.Log(game1.ToString());
        Debug.Log(game2.ToString());
        Debug.Log(turn.ToString());
        Debug.Log(players.ToString());

        StringAssert.AreEqualIgnoringCase("Game", game1.name.str);
        StringAssert.AreEqualIgnoringCase("Game", game2.name.str);
        StringAssert.AreEqualIgnoringCase("Turn", turn.name.str);

        Assert.AreEqual(5, G.GetEntityCount(), "Invalid entity count");
    }

    [Test]
    public void Find()
    {
        Assert.AreEqual(1, G.GetEntityCount(), "Invalid entity count");

        var game = G.Create("Game");
        Debug.Log(game);
        Assert.AreEqual(2, G.GetEntityCount(), "Invalid entity count");

        game = G.Find("Game");
        Assert.IsTrue(game.valid);

        game = G.Find("Hame");
        Debug.Log(game);
        Assert.IsFalse(game.valid);
        Assert.AreEqual(2, G.GetEntityCount(), "Invalid entity count");
    }

    [Test]
    public void GetSetValues()
    {
        Assert.AreEqual(1, G.GetEntityCount(), "Invalid entity count");

        var game = G.Create("Game");
        Assert.AreEqual(2, G.GetEntityCount(), "Invalid entity count");

        var turn = game["turn"].value.number;
        Assert.AreEqual(0, turn);
        G.Set(game, "turn", turn+1);
        turn = G.Get(game, "turn").number;
        Assert.AreEqual(1, turn);
    }

    [Test]
    public void SetPlayerNames()
    {
        var game = G.Create("game");

        var players = new [] 
        {
            G.Create("Player1"),
            G.Create("Player2"),
            G.Create("Player3"),
            G.Create("Player4"),
        };
        
        G.Set(players[0], "name", "Jo");
        G.Set(players[0], "points", 1);
        G.Set(players[1], "name", "SebP");
        G.Set(players[1], "points", 1);
        G.Set(players[2], "name", "SebG");
        G.Set(players[2], "points", 1);
        G.Set(players[3], "name", "Claudia");
        G.Set(players[3], "points", 1);

        G.Set(game, "players", players);

        Assert.AreEqual(15, G.GetEntityCount(), "Invalid entity count");

        Debug.Log(G.ToJSON());
    }

    [Test]
    public void FindRootNestedPathExceptions()
    {
        var players = G.Create("game").Create("players");
        players.Add(G.Create("p1").Set("name", "Arnold").Set("points", 3));
        players.Add(G.Create("p2").Set("name", "Schmidt").Set("points", 4));
        Assert.AreEqual(9, G.GetEntityCount(), "Invalid entity count");

        Assert.DoesNotThrow(() => G.Find("game", throwNotFound: true));
        Assert.DoesNotThrow(() => G.Find("p1", throwNotFound: true));
        Assert.DoesNotThrow(() => G.Find("p2", throwNotFound: true));
        Assert.DoesNotThrow(() => G.Find(G.Find("p2"), "name", throwNotFound: true));

        Assert.Throws<UnityException>(() => G.Find("app", throwNotFound: true));
        Assert.DoesNotThrow(() => G.Find("game.players[120].name", throwNotFound: false));
        Assert.Throws<UnityException>(() => G.Find("game.players[120].name", throwNotFound: true));
        Assert.Throws<UnityException>(() => G.Find("game.players[coucou].name", throwNotFound: true));
    }

    [Test]
    public void FindRootNestedPath()
    {
        var players = G.Create("game").Create("players");
        players.Add(G.Create("p1").Set("name", "Arnold").Set("points", 3));
        players.Add(G.Create("p2").Set("name", "Schmidt").Set("points", 4));
        Assert.AreEqual(9, G.GetEntityCount(), "Invalid entity count");

        var f = G.Find("game.players[1].name", throwNotFound: true);
        Assert.AreEqual("Schmidt", f.ToValue().ToString());
    }

    [Test]
    public void FindNestedPaths()
    { 
        var kPoints = new GKey("points");
        var players = G.Create("game").Create("players");
        var p1 = players.Add(G.Create("p1").Set("name", "Arnold").Set(kPoints, 1));
        var p2 = players.Add(G.Create("p2").Set("name", "Brice").Set(kPoints, 0));
        Assert.AreEqual(9, G.GetEntityCount(), "Invalid entity count");

        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"db.start: {G.ToJSON(DF)}");

        Assert.AreEqual(1, p1.Find(kPoints).ToNumber());

        p2.Set("points", 15);
        Assert.AreEqual(9, G.GetEntityCount(), "Invalid entity count");

        var f1 = G.Find("game.players[1].name");
        var f2 = p2.Find("name");
        Assert.AreEqual(f1, f2);
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, f1.ToJSON(DF));
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, players.Find("p1").ToJSON(DF));
    }

    [Test]
    public void SetEntityRefs()
    {
        var sm = G.Create("state_machine");
        var states = sm.Create("states");
        var idle = states.Create("Idle");
        var attack = states.Create("Attack");
        var move = states.Create("Move");

        var current = sm.Create("current");
        Assert.AreEqual(GHandle.nil, current.ToValue().ToHandle());
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"db.create: {G.ToJSON(DF)}");

        G.Set(current, idle);
        Assert.AreEqual("Idle", current.ToValue().ToHandle().name.str);
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"db.init: {G.ToJSON(DF)}");

        current.Set(attack);
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"db.attack: {G.ToJSON(DF)}");
        current.Set(idle);
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"db.idle: {G.ToJSON(DF)}");

        current.Set(move);
    }

    [Test]
    public void EnumerateStates()
    {
        var sm = G.Create("state_machine");
        var states = sm.Create("states");
        var idle = states.Create("Idle");
        var attack = states.Create("Attack");
        var move = states.Create("Move");
        var current = sm.Create("current");

        CollectionAssert.Contains(states, idle);
        CollectionAssert.Contains(states, attack);
        CollectionAssert.Contains(states, move);
        CollectionAssert.DoesNotContain(states, current);

        foreach (var s in states)
        { 
            Debug.Log(s);

            // This shouldn't affect the current enumeration
            states.Create("more_" + s.name.str);
        }

        Assert.AreEqual(6, states.Count);
        foreach (var s in states)
            Debug.Log(s);
    }

    [Test]
    public void HandleCollections()
    {
        var states = G.Create("states");
        states.Create("Idle");
        states.Create("Attack");
        states.Create("Move");

        ICollection<GHandle> coll = states;
        coll.Add(G.Create("Dead"));

        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, G.ToJSON(DF));

        Assert.AreEqual(4, coll.Count);
        Assert.IsTrue(coll.Contains(states.Find("Idle")));
        Assert.IsTrue(coll.Remove(G.Find("states[0]")));
        Assert.AreEqual(3, coll.Count);
        var handles = new GHandle[coll.Count];
        coll.CopyTo(handles, 0);
        CollectionAssert.AreEqual(coll, handles);

        coll.Clear();
        Assert.AreEqual(0, coll.Count);
    }

    [Test]
    public void HandleIndexing()
    {
        G.root["game"]["turn"].Set(4);
        Assert.AreEqual(4, G.Find("game.turn").ToValue().number);
        Assert.AreEqual(4, G.root["game"][0].ToValue().number);
    }

    [Test]
    public void BooleanValues()
    {
        var win = G.root["game"]["win"];
        win.Set(true);
        Assert.AreEqual("true", win.ToValue().ToString());
        Assert.AreEqual(1d, win.ToNumber());

        win.Set(false);
        Assert.AreEqual("false", win.ToValue().ToString());
        Assert.AreEqual(0d, win.ToNumber());
    }

    [Test]
    public void ThreadStress()
    {
        var game = G.Create("game");

        void Stressful()
        {
            var h = game;
            var rand = new System.Random();
            for (int i = 0; i < 10000; ++i)
            {
                var s = "";
                for (int c = 0; c < 4; c++)
                    s += (char)('A' + rand.Next(0, 6));
                var th = h.Create(s);
                var d = rand.Next(0, 8);
                if (d == 1)
                    h = th;
                else if (d == 2)
                    h = game;
                else if (d == 3)
                    th.Set(rand.NextDouble());
            }
        }

        var t1 = Task.Run(Stressful);
        var t2 = Task.Run(Stressful);
        var t3 = Task.Run(Stressful);

        Task.WaitAll(t1, t2, t3);
        Assert.IsNull(t1.Exception, t1.Exception?.Message);
        Assert.IsNull(t2.Exception, t1.Exception?.Message);
        Assert.IsNull(t3.Exception, t1.Exception?.Message);
    }

    [Test]
    public void SerializeEmptyDB()
    {
        using (var memoryStream = new MemoryStream())
        {
            Assert.IsTrue(G.Serialize(memoryStream, G.Diff()), "Failed to serialize DB");
            Assert.AreEqual(55, memoryStream.ToArray().Length);
        }
    }

    void SerializeAndDeserialize(int expectedSize)
    {
        byte[] bytes;
        using (var memoryStream = new MemoryStream())
        {
            Assert.IsTrue(G.Serialize(memoryStream, G.Diff()), "Failed to serialize DB");

            bytes = memoryStream.ToArray();
            Assert.AreEqual(expectedSize, bytes.Length);
        }

        G.Reset();
        Assert.AreEqual(1, G.GetEntityCount(), "Invalid entity count");

        using (var memoryStream = new MemoryStream(bytes))
            G.Apply(G.Deserialize(memoryStream));
    }

    [Test]
    public void SerializeDB()
    {
        var game = G.Create("game");
        game.Set("v1", "coucou");
        game.Set("v2", 66.6d);
        game.Set("v1", 32d);

        Assert.AreEqual(4, G.GetEntityCount(), "Invalid entity count");

        SerializeAndDeserialize(557);
        
        Assert.AreEqual(4, G.GetEntityCount(), "Invalid entity count");
        Assert.AreEqual(32d, G.Find("game.v1").ToNumber());
    }

    [Test]
    public void AddChildTransaction()
    {
        var kPoints = new GKey("points");
        var players = G.Create("game").Create("players");
        players.Add(G.Create("p1").Set("name", "Arnold").Set(kPoints, 1));
        players.Add(G.Create("p2").Set("name", "Brice").Set(kPoints, 0)).Set("points", 15);

        SerializeAndDeserialize(1240);

        Assert.AreEqual(15d, G.Find("game.players.p2.points").ToNumber());
    }

    [Test]
    public void ClearRemoveChildTransactions()
    {
        var states = G.Create("states");
        states.Create("Idle");
        states.Create("Attack");
        states.Create("Move");

        ICollection<GHandle> coll = states;
        coll.Clear();
        coll.Add(G.Create("Dead"));

        SerializeAndDeserialize(677);
    }

    void GValueEqualAssert(in GValue expected, in GValue v)
    {
        Assert.AreEqual(expected, v, "Values are not equal or were not implicitly casted");
    }

    [Test]
    public void ImplicitValueOperators()
    {
        var monster = G.Create("monster");
        var human = G.Create("human");
        monster.Set("name", "inc")
            .Set("scary", true)
            .Set("happy", false)
            .Set("age", 11d)
            .Set("version", 42U)
            .Set("enemy", human);

        // Equivalent entity get value (some are implicit)
        Assert.AreEqual(11d, monster["age"].value, 0d);
        Assert.AreEqual(11d, monster.Find("age").value, 0d);
        Assert.AreEqual(11d, monster.Get("age").number);
        Assert.AreEqual(11d, G.Find("monster.age").value, 0d);
        Assert.AreEqual(11d, G.Get(monster, "age"), 0d);
        Assert.AreEqual(11d, G.Get(monster, "age").number);

        // Get value for all supported types
        Assert.IsTrue(monster["scary"].value);
        Assert.IsFalse(monster["happy"].value);
        Assert.AreEqual(42, (int)monster["version"].value);
        Assert.AreEqual(42U, (uint)monster["version"].value);
        Assert.AreEqual(42d, monster["version"].value, 0d);
        Assert.AreEqual(human, (GHandle)monster["enemy"].value);
        Assert.AreEqual(human, monster["enemy"].value.ToHandle());
        StringAssert.AreEqualIgnoringCase("Inc", monster["name"].value);

        // Implicitly cast GValue from GHandle
        Assert.AreEqual(new GValue("inc"), (GValue)monster["name"]);
        GValueEqualAssert(new GValue("inc"), monster["name"]);
        GValueEqualAssert(new GValue(true), monster["scary"]);
        GValueEqualAssert(new GValue(false), monster["happy"]);
        GValueEqualAssert(new GValue(11d), monster["age"]);
        GValueEqualAssert(new GValue(42U), monster["version"]);
        GValueEqualAssert(new GValue(human), monster["enemy"]);
    }
}
