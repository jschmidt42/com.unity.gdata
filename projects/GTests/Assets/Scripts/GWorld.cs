using UnityEngine;

public class GWorld : MonoBehaviour
{
    public GHandle game;
    public GHandle[] players;

    void Start()
    {
        //G.Reset();
        game = G.Create("game");
        var pes = game.Create("players");

        players = new GHandle[]
        {
            pes.Create("Player 1"),
            pes.Create("Player 2"),
            pes.Create("Player 3"),
            pes.Create("Player 4")
        };

        players[0].Set("Jonathan");
        players[1].Set("points", 44);
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        G.Reset();
    }
}
