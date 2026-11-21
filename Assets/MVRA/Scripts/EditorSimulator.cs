using UnityEngine;

namespace MVRA
{
    public class EditorSimulator : MonoBehaviour
    {
        public Transform rig;
        public Transform cameraAR;
        public float velocidade = 8f;
        public float sensibilidade = 3f;
        public float alturaOlhos = 1.5f;

        float yaw, pitch;

        void Start()
        {
            if (!Application.isEditor) { enabled = false; return; }
            if (cameraAR == null && Camera.main != null) cameraAR = Camera.main.transform;
            if (rig == null && cameraAR != null) rig = cameraAR.root;
            if (rig != null) rig.position = new Vector3(rig.position.x, alturaOlhos, rig.position.z);
            if (rig != null) yaw = rig.eulerAngles.y;
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (rig == null) return;
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * sensibilidade;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensibilidade, -60f, 60f);
            }
            rig.rotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 mov = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) mov += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) mov += Vector3.back;
            if (Input.GetKey(KeyCode.A)) mov += Vector3.left;
            if (Input.GetKey(KeyCode.D)) mov += Vector3.right;
            float v = velocidade * (Input.GetKey(KeyCode.LeftShift) ? 4f : 1f);
            Vector3 frente = Quaternion.Euler(0f, yaw, 0f) * mov;
            rig.position += frente * v * Time.deltaTime;
#endif
        }
    }
}
