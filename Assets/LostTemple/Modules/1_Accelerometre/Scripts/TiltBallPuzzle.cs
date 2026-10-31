using UnityEngine;

namespace LostTemple
{
    /// <summary>Salle 1 — Accéléromètre : incliner le téléphone pour guider une boule sur un pont étroit.</summary>
    public class TiltBallPuzzle : PuzzleStep
    {
        public Rigidbody ball;
        public Transform spawn;
        public Collider goal;
        public Renderer goalPlate;
        public float acceleration = 14f;
        public float killY = -3f;

        float bestZ;
        int falls;

        void Reset() { sensor = SensorKind.Accelerometer; }

        protected override void OnBegin()
        {
            ball.isKinematic = false;
            Respawn();
        }

        void FixedUpdate()
        {
            if (!IsActive || ball == null) return;
            Vector2 tilt = Sensors.GetTilt();
            ball.AddForce(new Vector3(tilt.x, 0f, tilt.y) * acceleration, ForceMode.Acceleration);
        }

        void Update()
        {
            if (!IsActive) return;
            if (ball.position.y < killY)
            {
                falls++;
                GameManager.Instance.AddFall();
                Audio.Play(Sfx.Fail);
                UIManager.Instance.ShowToast(falls < 3 ? "La boule est tombée… elle revient au départ." : "Astuce : réduisez la sensibilité dans Pause.");
                Respawn();
            }
            // Progression = distance parcourue vers l'arrivée
            float z = ball.position.z;
            if (z > bestZ + 0.5f) { bestZ = z; ReportProgress(); }
            if (goalPlate != null)
                SetGlow(goalPlate, new Color(1f, 0.7f, 0.2f), 0.6f + 0.4f * Mathf.Sin(Time.time * 3f));
        }

        void Respawn()
        {
            ball.linearVelocity = Vector3.zero;
            ball.angularVelocity = Vector3.zero;
            ball.position = spawn.position;
            ball.transform.position = spawn.position;
            bestZ = spawn.position.z;
        }

        public void OnBallEnteredGoal()
        {
            if (!IsActive) return;
            ball.linearVelocity *= 0.2f;
            SetGlow(goalPlate, new Color(1f, 0.8f, 0.3f), 3f);
            Complete();
            ball.isKinematic = true;
        }
    }
}
