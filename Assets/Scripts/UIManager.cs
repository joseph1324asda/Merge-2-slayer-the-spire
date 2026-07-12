using Merge2.Core;
using UnityEngine;

namespace Merge2.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject mergeBoardPanel;
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private GameObject adRevivePopup;

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            GameManager.OnRewardChoicesRerolled += HandleRewardChoicesRerolled;

            if (GameManager.Instance != null)
            {
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.OnRewardChoicesRerolled -= HandleRewardChoicesRerolled;
        }

        public void OnClick_StartRun()
        {
            GameManager.Instance?.StartRun();
        }

        public void OnClick_StartCombat()
        {
            GameManager.Instance?.StartCombat();
        }

        public void OnClick_RerollRewardsAd()
        {
            GameManager.Instance?.RerollRewardsAfterAd();
        }

        public void OnClick_ReviveAd()
        {
            GameManager.Instance?.TryReviveAfterAd();
        }

        private void HandleGameStateChanged(GameState state)
        {
            Debug.Log("[UIManager] Updating panels for state: " + state);

            SetPanelActive(mainMenuPanel, state == GameState.OutOfGame);
            SetPanelActive(mapPanel, state == GameState.MapRouting);
            SetPanelActive(mergeBoardPanel, state == GameState.MergePhase);
            SetPanelActive(combatPanel, state == GameState.CombatPhase || state == GameState.DeathPenaltyPhase);
            SetPanelActive(rewardPanel, state == GameState.RewardPhase);
            SetPanelActive(adRevivePopup, state == GameState.DeathPenaltyPhase);
        }

        private void HandleRewardChoicesRerolled()
        {
            Debug.Log("[UIManager] Reward choices rerolled. Refresh reward visuals here.");
        }

        private static void SetPanelActive(GameObject panel, bool isActive)
        {
            if (panel == null || panel.activeSelf == isActive)
            {
                return;
            }

            panel.SetActive(isActive);
        }
    }
}
