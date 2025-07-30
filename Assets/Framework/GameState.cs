using System;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// The different states of the game
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Settings,
        Credits
    }

    /// <summary>
    /// Event launched when the game state changes
    /// </summary>
    [Serializable]
    public class GameStateChangedEvent
    {
        public GameState previousState;
        public GameState newState;
        public float timestamp;

        public GameStateChangedEvent(GameState previous, GameState newState)
        {
            this.previousState = previous;
            this.newState = newState;
            this.timestamp = Time.time;
        }
    }
}