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
    private Vector3 originalPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cam != null)
        {
            originalFOV = cam.Lens.FieldOfView;
            originalPosition = cam.transform.position;
            noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
        else
        {
            Debug.LogWarning("CinemachineCamera is not assigned in the CameraManager inspector!");
        }
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
        Vector3 targetPos = new Vector3(targetXY.x, targetXY.y, cam.transform.position.z);
        moveCoroutine = StartCoroutine(MoveCoroutine(targetPos, speed));
    }

    public void Offset(Vector2 offsetXY, float speed)
    {
        if (cam == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        Vector3 targetPos = cam.transform.position + new Vector3(offsetXY.x, offsetXY.y, 0);
        moveCoroutine = StartCoroutine(MoveCoroutine(targetPos, speed));
    }

    public void Shake(float intensity, float duration)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    public void ResetCamera(float speed)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        // Only resetting zoom according to user requirement
        zoomCoroutine = StartCoroutine(ZoomCoroutine(originalFOV, speed));

        // In the future:
        // if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        // moveCoroutine = StartCoroutine(MoveCoroutine(originalPosition, speed));
    }

    private IEnumerator ZoomCoroutine(float targetFOV, float speed)
    {
        if (cam == null) yield break;
        
        float startFOV = cam.Lens.FieldOfView;
        float time = 0;
        
        // Ensure duration is based on a sensible calculation, e.g. speed 1 means 1 second.
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

    private IEnumerator MoveCoroutine(Vector3 targetPos, float speed)
    {
        if (cam == null) yield break;

        Vector3 startPos = cam.transform.position;
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, time);
            yield return null;
        }
        cam.transform.position = targetPos;
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
            // Fallback if no noise component is attached to the CinemachineCamera
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
