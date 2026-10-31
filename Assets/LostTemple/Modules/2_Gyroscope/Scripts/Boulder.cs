using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Rocher géant qui se met à rouler derrière l'explorateur quand la salle est terminée :
    /// il faut courir jusqu'à la porte. S'il rattrape l'explorateur, il le renvoie en arrière et repart.
    /// </summary>
    public class Boulder : MonoBehaviour, IRoomDoneListener
    {
        public static bool Rolling { get; private set; }

        public Transform rock;
        public float speed = 3.4f;
        public float endZ = 9.5f;
        public ParticleSystem crashDust;

        float radius;

        void Start()
        {
            radius = rock.lossyScale.x * 0.5f;
            rock.gameObject.SetActive(false);
        }

        void OnDestroy() { Rolling = false; }

        public void OnRoomDone() => StartChase();

        public void StartChase()
        {
            StartCoroutine(Chase());
        }

        IEnumerator Chase()
        {
            var p = PlayerController.Instance;
            if (p == null) yield break;
            rock.gameObject.SetActive(true);
            Place(p.transform.position.z - 8f);
            Rolling = true;
            AudioManager.Instance.Tension = 1f;
            AudioManager.Instance.Play(Sfx.Roar, 0.5f);
            CameraRig.Shake(0.4f);
            UIManager.Instance.ShowRoomTitle("Un rocher géant ! Courez jusqu'à la porte !");
            float k = GameManager.Instance.HazardSpeed > 1f ? 1.15f : 1f;
            while (rock.position.z < endZ)
            {
                if (p == null) yield break;
                if (!p.Frozen)
                {
                    float dz = speed * k * Time.deltaTime;
                    rock.position += Vector3.forward * dz;
                    rock.Rotate(Vector3.right, dz / radius * Mathf.Rad2Deg, Space.World);
                    CameraRig.Shake(0.04f);
                    AudioManager.Instance.SetLoopIntensity(0.8f);
                    Vector3 d = p.transform.position - rock.position;
                    d.y = 0f;
                    if (d.magnitude < radius + 0.3f)
                    {
                        p.Hurt("Écrasé par le rocher ! Courez plus vite !");
                        Place(p.transform.position.z - 7f);
                    }
                }
                yield return null;
            }
            Rolling = false;
            AudioManager.Instance.Tension = 0f;
            AudioManager.Instance.SetLoopIntensity(0f);
            AudioManager.Instance.Play(Sfx.Mechanism);
            CameraRig.Shake(0.5f);
            if (crashDust != null) { crashDust.transform.position = rock.position; crashDust.Play(); }
            rock.gameObject.SetActive(false);
        }

        void Place(float z)
        {
            rock.position = new Vector3(rock.position.x, radius, z);
        }
    }
}
