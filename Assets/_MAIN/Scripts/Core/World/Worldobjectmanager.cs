using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldObjectManager : MonoBehaviour
{
    public static WorldObjectManager instance { get; private set; }

    private Dictionary<string, GameObject> registry = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // ??? Регистрация ????????????????????????????????????????????????

    public void Register(string id, GameObject go)
    {
        if (go == null)
        {
            Debug.LogWarning($"[WorldObjectManager] Попытка зарегистрировать null под именем '{id}'");
            return;
        }

        id = id.ToLower();

        if (!registry.ContainsKey(id))
            registry.Add(id, go);
        else
            registry[id] = go;
    }

    public void Unregister(string id) => registry.Remove(id.ToLower());

    public void UnregisterAll(IEnumerable<string> ids)
    {
        foreach (var id in ids)
            Unregister(id);
    }

    public bool TryGet(string id, out GameObject go) =>
        registry.TryGetValue(id.ToLower(), out go);

    // ??? Команды ????????????????????????????????????????????????????

    public void SetActive(string id, bool active)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        go.SetActive(active);
    }

    public void PlayAnim(string id, string stateName)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning($"[WorldObjectManager] '{id}' не имеет Animator"); return; }

        anim.Play(stateName);
    }

    public void SetAnimatorBool(string id, string param, bool value)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning($"[WorldObjectManager] '{id}' не имеет Animator"); return; }

        anim.SetBool(param, value);
    }

    public void SetPosition(string id, Vector3 position)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        go.transform.position = position;
    }

    public Coroutine MoveTo(string id, Vector3 target, float speed)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return null; }

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            return StartCoroutine(MovingToRb(rb, go, target, speed));

        return StartCoroutine(MovingTo(go, target, speed));
    }

    // Для объектов с Rigidbody2D — двигаем через velocity
    private IEnumerator MovingToRb(Rigidbody2D rb, GameObject go, Vector3 target, float speed)
    {
        Vector2 target2D = new Vector2(target.x, target.y);

        while (Vector2.Distance(rb.position, target2D) > 0.05f)
        {
            Vector2 dir = (target2D - rb.position).normalized;
            rb.linearVelocity = new Vector2(dir.x * speed, rb.linearVelocity.y);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.position = target2D;
    }

    // Для обычных объектов без физики
    private IEnumerator MovingTo(GameObject go, Vector3 target, float speed)
    {
        while (Vector3.Distance(go.transform.position, target) > 0.01f)
        {
            go.transform.position = Vector3.MoveTowards(
                go.transform.position,
                target,
                speed * Time.deltaTime
            );
            yield return null;
        }
        go.transform.position = target;
    }

    public void Flip(string id)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        Vector3 s = go.transform.localScale;
        s.x *= -1;
        go.transform.localScale = s;
    }

    // --- Animator (new) ---

    public void SetAnimatorTrigger(string id, string param)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning($"[WorldObjectManager] '{id}' no Animator"); return; }
        anim.SetTrigger(param);
    }

    public void SetAnimatorFloat(string id, string param, float value)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning($"[WorldObjectManager] '{id}' no Animator"); return; }
        anim.SetFloat(param, value);
    }

    public void SetAnimatorInt(string id, string param, int value)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return; }
        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning($"[WorldObjectManager] '{id}' no Animator"); return; }
        anim.SetInteger(param, value);
    }

    // --- Transform (new) ---

    public Coroutine RotateTo(string id, Vector3 targetEuler, float speed)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return null; }
        return StartCoroutine(RotatingTo(go, targetEuler, speed));
    }

    private IEnumerator RotatingTo(GameObject go, Vector3 targetEuler, float speed)
    {
        Quaternion target = Quaternion.Euler(targetEuler);
        while (Quaternion.Angle(go.transform.rotation, target) > 0.1f)
        {
            go.transform.rotation = Quaternion.RotateTowards(
                go.transform.rotation, target, speed * Time.deltaTime);
            yield return null;
        }
        go.transform.rotation = target;
    }

    public Coroutine ScaleTo(string id, Vector3 targetScale, float speed)
    {
        if (!TryGet(id, out var go)) { LogNotFound(id); return null; }
        return StartCoroutine(ScalingTo(go, targetScale, speed));
    }

    private IEnumerator ScalingTo(GameObject go, Vector3 targetScale, float speed)
    {
        while (Vector3.Distance(go.transform.localScale, targetScale) > 0.001f)
        {
            go.transform.localScale = Vector3.MoveTowards(
                go.transform.localScale, targetScale, speed * Time.deltaTime);
            yield return null;
        }
        go.transform.localScale = targetScale;
    }

    // ??? Утилита ????????????????????????????????????????????????????

    private void LogNotFound(string id) =>
        Debug.LogWarning($"[WorldObjectManager] Объект '{id}' не найден в реестре");
}
