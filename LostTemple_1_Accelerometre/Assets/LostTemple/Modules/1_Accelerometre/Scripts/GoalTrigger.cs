using UnityEngine;

namespace LostTemple
{
    /// <summary>Zone d'arrivée de la boule (salle 1).</summary>
    [RequireComponent(typeof(Collider))]
    public class GoalTrigger : MonoBehaviour
    {
        public TiltBallPuzzle puzzle;

        void OnTriggerEnter(Collider other)
        {
            if (puzzle != null && other.attachedRigidbody == puzzle.ball) puzzle.OnBallEnteredGoal();
        }
    }
}
