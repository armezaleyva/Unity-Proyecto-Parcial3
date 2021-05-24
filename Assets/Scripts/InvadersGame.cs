using System;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Connection;
using MLAPI.NetworkVariable;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class InvadersGame : NetworkBehaviour
{

    [Header("UI Settings")]
    public TMP_Text gameTimerText;

    [SerializeField]
    [Tooltip("Time Remaining until the game starts")]
    private float m_DelayedStartTime = 5.0f;

    [SerializeField]
    private NetworkVariableFloat m_TickPeriodic = new NetworkVariableFloat(0.2f);
    //These help to simplify checking server vs client
    //[NSS]: This would also be a great place to add a state machine and use networked vars for this
    private bool m_ClientGameOver;
    private bool m_ClientGameStarted;
    private bool m_ClientStartCountdown;

    private NetworkVariableBool m_CountdownStarted = new NetworkVariableBool(false);

    private float m_NextTick;

    // the timer should only be synced at the beginning
    // and then let the client to update it in a predictive manner
    private NetworkVariableFloat m_ReplicatedTimeRemaining = new NetworkVariableFloat();
    private float m_TimeRemaining;

    public static InvadersGame Singleton { get; private set; }

    public NetworkVariableBool hasGameStarted { get; } = new NetworkVariableBool(false);

    public NetworkVariableBool isGameOver { get; } = new NetworkVariableBool(false);

    /// <summary>
    ///     Awake
    ///     A good time to initialize server side values
    /// </summary>
    private void Awake()
    {
        // TODO: Improve this singleton pattern
        Singleton = this;
        OnSingletonReady?.Invoke();

        if (IsServer)
        {
            hasGameStarted.Value = false;

            //Set our time remaining locally
            m_TimeRemaining = m_DelayedStartTime;

            //Set for server side
            m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
        }
        else
        {
            //We do a check for the client side value upon instantiating the class (should be zero)
            Debug.LogFormat("Client side we started with a timer value of {0}", m_ReplicatedTimeRemaining.Value);
        }
    }

    /// <summary>
    ///     Update
    ///     MonoBehaviour Update method
    /// </summary>
    private void Update()
    {
        //Is the game over?
        if (IsCurrentGameOver()) return;

        //Update game timer (if the game hasn't started)
        UpdateGameTimer();

        //If we are a connected client, then don't update the enemies (server side only)
        if (!IsServer) return;
    }

    /// <summary>
    ///     OnDestroy
    ///     Clean up upon destruction of this class
    /// </summary>
    protected void OnDestroy()
    {
        if (IsServer)
        {
        }
    }

    internal static event Action OnSingletonReady;

    public override void NetworkStart()
    {
        if (IsClient && !IsServer)
        {
            m_ClientGameOver = false;
            m_ClientStartCountdown = false;
            m_ClientGameStarted = false;

            m_ReplicatedTimeRemaining.OnValueChanged += (oldAmount, newAmount) =>
            {
                // See the ShouldStartCountDown method for when the server updates the value
                if (m_TimeRemaining == 0)
                {
                    Debug.LogFormat("Client side our first timer update value is {0}", newAmount);
                    m_TimeRemaining = newAmount;
                }
                else
                {
                    Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", m_ReplicatedTimeRemaining.Value);
                }
            };

            m_CountdownStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientStartCountdown = newValue;
                Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
            };

            hasGameStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameStarted = newValue;
                gameTimerText.gameObject.SetActive(!m_ClientGameStarted);
                Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
            };

            isGameOver.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameOver = newValue;
                Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
            };
        }

        //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
        //and in turn makes the players visible and allows for the players to be controlled.
        //SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

        base.NetworkStart();
    }

    /// <summary>
    ///     ShouldStartCountDown
    ///     Determines when the countdown should start
    /// </summary>
    /// <returns>true or false</returns>
    private bool ShouldStartCountDown()
    {
        //If the game has started, then don't both with the rest of the count down checks.
        if (HasGameStarted()) return false;
        if (IsServer)
        {
            //SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();
            m_CountdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();

            //While we are counting down, continually set the m_ReplicatedTimeRemaining.Value (client should only receive the update once)
            if (m_CountdownStarted.Value && m_ReplicatedTimeRemaining.Settings.SendTickrate != -1)
            {
                //Now we can specify that we only want this to be sent once
                m_ReplicatedTimeRemaining.Settings.SendTickrate = -1;

                //Now set the value for our one time m_ReplicatedTimeRemaining networked var for clients to get updated once
                m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
            }

            return m_CountdownStarted.Value;
        }

        return m_ClientStartCountdown;
    }

    /// <summary>
    ///     IsCurrentGameOver
    ///     Returns whether the game is over or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool IsCurrentGameOver()
    {
        if (IsServer)
            return isGameOver.Value;
        return m_ClientGameOver;
    }

    /// <summary>
    ///     HasGameStarted
    ///     Determine whether the game has started or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool HasGameStarted()
    {
        if (IsServer)
            return hasGameStarted.Value;
        return m_ClientGameStarted;
    }

    /// <summary>
    ///     Client side we try to predictively update the gameTimer
    ///     as there shouldn't be a need to receive another update from the server
    ///     We only got the right m_TimeRemaining value when we started so it will be enough
    /// </summary>
    /// <returns> True when m_HasGameStared is set </returns>
    private void UpdateGameTimer()
    {
        if (!ShouldStartCountDown()) return;
        if (!HasGameStarted() && m_TimeRemaining > 0.0f)
        {
            m_TimeRemaining -= Time.deltaTime;

            if (IsServer) // Only the server should be updating this
            {
                if (m_TimeRemaining <= 0.0f)
                {
                    m_TimeRemaining = 0.0f;
                    hasGameStarted.Value = true;
                    OnGameStarted();
                }

                m_ReplicatedTimeRemaining.Value = m_TimeRemaining;
            }

            if (m_TimeRemaining > 0.1f)
                gameTimerText.SetText("{0}", Mathf.FloorToInt(m_TimeRemaining));
        }
    }

    /// <summary>
    ///     OnGameStarted
    ///     Only invoked by the server, this hides the timer text and initializes the aliens and level
    /// </summary>
    private void OnGameStarted()
    {
        gameTimerText.gameObject.SetActive(false);
    }

    public void SetGameEnd(bool isGameOver)
    {
        Assert.IsTrue(IsServer, "SetGameEnd should only be called server side!");

        // We should only end the game if all the player's are dead
        if (isGameOver)
        {
            foreach (NetworkClient networkedClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = networkedClient.PlayerObject;
                if(playerObject == null) continue;
            }
        }
        this.isGameOver.Value = isGameOver;
    }

    public void ExitGame()
    {
        if (IsServer) NetworkManager.Singleton.StopServer();
        if (IsClient) NetworkManager.Singleton.StopClient();
        SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
    }
}
