using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera cam;
    private CinemachineBasicMultiChannelPerlin noise;

    private Coroutine zoomCoroutine;
    private Coroutine moveCoroutine;
    private Coroutine shakeCoroutine;

    private float originalFOV;
    private Vector4? cameraBounds = null; // minX, maxX, minY, maxY

    // Невидимая цель для камеры (назначается в инспекторе)
    [SerializeField] private Transform dummyTarget;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cam != null)
        {
            originalFOV = cam.Lens.FieldOfView;
            noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();

            // Включаем максимальную резкость на самой камере
            var followComp = cam.GetComponent<CinemachineFollow>();
            if (followComp != null)
            {
                followComp.TrackerSettings.PositionDamping = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("CinemachineCamera is not assigned in the CameraManager inspector!");
        }
    }

    private void Start()
    {
        if (cam == null || dummyTarget == null) return;

        // Наводим камеру на пустышку
        cam.Follow = dummyTarget;
    }

    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        cameraBounds = new Vector4(minX, maxX, minY, maxY);
    }

    public void RemoveBounds()
    {
        cameraBounds = null;
    }

    public void Zoom(float targetFOV, float speed)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomCoroutine(targetFOV, speed));
    }

    public void MoveTo(Vector2 targetXY, float speed)
    {
        if (cam == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        dummyTarget.SetParent(null); // отрываем пустышку от актера

        Vector3 targetPos = new Vector3(targetXY.x, targetXY.y, dummyTarget.position.z);
        moveCoroutine = StartCoroutine(MoveCoroutine(ClampPosition(targetPos), speed));
    }

    public void Offset(Vector2 offsetXY, float speed)
    {
        if (cam == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        dummyTarget.SetParent(null); // отрываем пустышку от актера

        Vector3 targetPos = dummyTarget.position + new Vector3(offsetXY.x, offsetXY.y, 0);
        moveCoroutine = StartCoroutine(MoveCoroutine(ClampPosition(targetPos), speed));
    }

    public void Follow(Transform target, float speed)
    {
        if (cam == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        Debug.Log($"[CameraManager Follow] Плавно летим пустышкой к {target.name}");
        moveCoroutine = StartCoroutine(FollowCoroutine(target, speed));
    }

    public void StopFollow(float speed)
    {
        if (cam == null) return;

        Debug.Log("[CameraManager StopFollow] Сброс слежения (возврат пустышки на актера 1)...");
        if (WorldObjectManager.instance != null && WorldObjectManager.instance.TryGet("1", out GameObject go))
        {
            Follow(go.transform, speed);
        }
        else
        {
            // Если игрок 1 удален со сцены, просто оставляем камеру стоять на месте
            dummyTarget.SetParent(null);
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        }
    }

    public void LookAt(Transform target, float speed, float duration)
    {
        if (cam == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        // Запоминаем, за кем мы следили (возможно пустышка сейчас child Игрока)
        Transform prevParent = dummyTarget.parent;
        Vector3 prevPos = dummyTarget.position;
        dummyTarget.SetParent(null);

        moveCoroutine = StartCoroutine(LookAtCoroutine(target, prevParent, prevPos, speed, duration));
    }

    public void Shake(float intensity, float duration)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    public void ResetZoom(float speed)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomCoroutine(originalFOV, speed));
    }

    public void ResetPos(float speed)
    {
        StopFollow(speed);
    }

    public void ResetCamera(float speed)
    {
        ResetZoom(speed);
        ResetPos(speed);
    }

    private Vector3 ClampPosition(Vector3 pos)
    {
        if (cameraBounds.HasValue)
        {
            Vector4 b = cameraBounds.Value;
            pos.x = Mathf.Clamp(pos.x, b.x, b.y);
            pos.y = Mathf.Clamp(pos.y, b.z, b.w);
        }
        return pos;
    }

    private IEnumerator ZoomCoroutine(float targetFOV, float speed)
    {
        if (cam == null) yield break;

        float startFOV = cam.Lens.FieldOfView;
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            var lens = cam.Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, time);
            cam.Lens = lens;
            yield return null;
        }
        var finalLens = cam.Lens;
        finalLens.FieldOfView = targetFOV;
        cam.Lens = finalLens;
    }

    private IEnumerator FollowCoroutine(Transform target, float speed)
    {
        // Отрываем пустышку, чтобы она плавно перелетела
        dummyTarget.SetParent(null);
        Vector3 startPos = dummyTarget.position;
        float time = 0;

        while (time < 1f)
        {
            if (target == null) yield break; // если актера удалили в процессе

            time += Time.deltaTime * speed;
            Vector3 targetPos = ClampPosition(new Vector3(target.position.x, target.position.y, startPos.z));
            dummyTarget.position = Vector3.Lerp(startPos, targetPos, time);
            yield return null;
        }

        if (target != null)
        {
            // Жестко примагничиваем пустышку к центру актера
            dummyTarget.position = ClampPosition(new Vector3(target.position.x, target.position.y, startPos.z));
            dummyTarget.SetParent(target);
            Debug.Log($"[CameraManager Follow] Пустышка прикреплена к {target.name}");
        }
    }

    private IEnumerator MoveCoroutine(Vector3 targetPos, float speed)
    {
        Vector3 startPos = dummyTarget.position;
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            dummyTarget.position = Vector3.Lerp(startPos, targetPos, time);
            yield return null;
        }
        dummyTarget.position = targetPos;
    }

    private IEnumerator LookAtCoroutine(Transform target, Transform prevParent, Vector3 returnPos, float speed, float duration)
    {
        Vector3 startPos = dummyTarget.position;
        float time = 0;

        // Летим к цели (или к её текущим координатам, если она движется)
        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            if (target != null)
            {
                Vector3 targetPos = ClampPosition(new Vector3(target.position.x, target.position.y, startPos.z));
                dummyTarget.position = Vector3.Lerp(startPos, targetPos, time);
            }
            yield return null;
        }

        if (target != null)
            dummyTarget.position = ClampPosition(new Vector3(target.position.x, target.position.y, startPos.z));

        // Ждем
        yield return new WaitForSeconds(duration);

        // Летим обратно
        time = 0;
        startPos = dummyTarget.position;
        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            // Если мы до этого следили за персонажем, то летим к его текущим координатам
            Vector3 targetPos = prevParent != null ? prevParent.position : returnPos;
            targetPos = ClampPosition(new Vector3(targetPos.x, targetPos.y, startPos.z));

            dummyTarget.position = Vector3.Lerp(startPos, targetPos, time);
            yield return null;
        }

        // Восстанавливаем статус-кво
        if (prevParent != null)
        {
            dummyTarget.SetParent(prevParent);
            dummyTarget.position = prevParent.position; // примагничиваемся
        }
        else
        {
            dummyTarget.position = ClampPosition(new Vector3(returnPos.x, returnPos.y, startPos.z));
        }
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        if (cam == null) yield break;

        if (noise != null)
        {
            noise.AmplitudeGain = intensity;
            yield return new WaitForSeconds(duration);
            noise.AmplitudeGain = 0;
        }
        else
        {
            Vector3 startPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = Random.Range(-1f, 1f) * intensity;
                float offsetY = Random.Range(-1f, 1f) * intensity;

                cam.transform.position = startPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            cam.transform.position = startPos;
        }
    }
}

