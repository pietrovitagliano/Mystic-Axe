// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(Animator))]
    public class AxeAnimatorHandler : MonoBehaviour
    {
        #region Animator Parameters
        private Animator animator;
        private int rotateDuringThunderstormStartHash;
        private int rotateDuringThunderstormEndHash;
        #endregion

        #region Public Modifiers Parameters
        public Animator Animator { get => animator; }
        public int RotateDuringThunderstormStartHash { get => rotateDuringThunderstormStartHash; }
        public int RotateDuringThunderstormEndHash { get => rotateDuringThunderstormEndHash; }
        #endregion

        private void Awake()
        {
            InizializeAnimatorVariables();
        }
        
        private void InizializeAnimatorVariables()
        {
            animator = GetComponent<Animator>();

            rotateDuringThunderstormStartHash = Animator.StringToHash(Utils.rotateDuringThunderstormStartParam);
            rotateDuringThunderstormEndHash = Animator.StringToHash(Utils.rotateDuringThunderstormEndParam);
        }
    }
}
