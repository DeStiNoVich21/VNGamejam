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

    private bool TryGet(string id, out GameObject go) =>
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
        return StartCoroutine(MovingTo(go, target, speed));
    }

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

    // ??? Утилита ????????????????????????????????????????????????????

    private void LogNotFound(string id) =>
        Debug.LogWarning($"[WorldObjectManager] Объект '{id}' не найден в реестре");
}