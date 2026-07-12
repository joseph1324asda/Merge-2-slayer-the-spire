using System;
using System.Collections;
using Merge2.Combat;
using UnityEngine;

namespace Merge2.Core
{
    public enum GameState
    {
        OutOfGame,
        MapRouting,
        MergePhase,
        CombatPhase,
        RewardPhase,
        DeathPenaltyPhase
    }

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private float deathPenaltySeconds = 10f;
        [SerializeField, Range(0.01f, 1f)] private float adReviveHealthPercent = 0.5f;
        [SerializeField] private GameState initialState = GameState.OutOfGame;
        [SerializeField] private GameState reviveReturnState = GameState.CombatPhase;

        private Coroutine deathPenaltyRoutine;

        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnRewardChoicesRerolled;

        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate instance found. Destroying duplicate on " + gameObject.name + ".");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentState = initialState;
        }

        private void Start()
        {
            Debug.Log("[GameManager] Initial state: " + CurrentState);
            OnGameStateChanged?.Invoke(CurrentState);
            EnterState(CurrentState);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState)
            {
                Debug.Log("[GameManager] ChangeState ignored. Already in " + newState + ".");
                return;
            }

            ExitState(CurrentState);

            GameState previousState = CurrentState;
            CurrentState = newState;

            Debug.Log("[GameManager] State changed: " + previousState + " -> " + CurrentState);
            OnGameStateChanged?.Invoke(CurrentState);

            EnterState(CurrentState);
        }

        public void StartRun()
        {
            ChangeState(GameState.MapRouting);
        }

        public void StartCombat()
        {
            ChangeState(GameState.CombatPhase);
        }

        public void RerollRewardsAfterAd()
        {
            if (CurrentState != GameState.RewardPhase)
            {
                Debug.LogWarning("[GameManager] Reward reroll ignored outside RewardPhase.");
                return;
            }

            Debug.Log("[GameManager] Reward reroll ad completed. Refreshing reward choices.");
            OnRewardChoicesRerolled?.Invoke();
        }

        public bool TryReviveAfterAd()
        {
            if (CurrentState != GameState.DeathPenaltyPhase)
            {
                Debug.LogWarning("[GameManager] Revive ad ignored outside DeathPenaltyPhase.");
                return false;
            }

            StopDeathPenaltyTimer();

            CombatEntity player = combatManager != null ? combatManager.Player : null;
            if (player == null)
            {
                Debug.LogWarning("[GameManager] Revive ad completed, but no combat player was assigned.");
                ChangeState(reviveReturnState);
                return true;
            }

            int reviveHealth = Mathf.Max(1, Mathf.CeilToInt(player.MaxHP * adReviveHealthPercent));
            player.Revive(reviveHealth);

            Debug.Log("[GameManager] Revive ad completed. Player revived at " + player.CurrentHP + "/" + player.MaxHP + " HP.");
            ChangeState(reviveReturnState);
            return true;
        }

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.CombatPhase:
                    combatManager?.StartCombat();
                    break;
                case GameState.DeathPenaltyPhase:
                    combatManager?.StopCombat();
                    StartDeathPenaltyTimer();
                    break;
                case GameState.RewardPhase:
                case GameState.OutOfGame:
                    combatManager?.StopCombat();
                    break;
            }
        }

        private void ExitState(GameState state)
        {
            if (state == GameState.DeathPenaltyPhase)
            {
                StopDeathPenaltyTimer();
            }
        }

        private void StartDeathPenaltyTimer()
        {
            StopDeathPenaltyTimer();
            deathPenaltyRoutine = StartCoroutine(DeathPenaltyTimer());
            Debug.Log("[GameManager] Death penalty timer started for " + deathPenaltySeconds + " seconds.");
        }

        private void StopDeathPenaltyTimer()
        {
            if (deathPenaltyRoutine == null)
            {
                return;
            }

            StopCoroutine(deathPenaltyRoutine);
            deathPenaltyRoutine = null;
            Debug.Log("[GameManager] Death penalty timer stopped.");
        }

        private IEnumerator DeathPenaltyTimer()
        {
            yield return new WaitForSeconds(deathPenaltySeconds);

            deathPenaltyRoutine = null;

            if (CurrentState != GameState.DeathPenaltyPhase)
            {
                yield break;
            }

            ClearPlayerMergeBoard();
            Debug.Log("[GameManager] Death penalty expired. Player merge board cleared.");
            ChangeState(GameState.OutOfGame);
        }

        private void ClearPlayerMergeBoard()
        {
            MergeBoard board = combatManager != null && combatManager.Player != null ? combatManager.Player.Board : null;
            if (board == null)
            {
                Debug.LogWarning("[GameManager] No player merge board available to clear.");
                return;
            }

            board.Clear();
        }
    }
}
