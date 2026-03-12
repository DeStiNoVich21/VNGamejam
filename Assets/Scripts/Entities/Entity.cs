using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using UnityEngine;
using Systems;

[DefaultExecutionOrder(10)]
public class Entity : SerializedMonoBehaviour
{
    public List<IComponent> Components = new();
    public List<ISystem> Systems = new();

    [HideInInspector] public Action OnUpdate;
    [HideInInspector] public Action OnFixedUpdate;
    [HideInInspector] public Action OnLateUpdate;
    [HideInInspector] public event Action OnGizmosUpdate;

   public virtual void OnEnable()
    {
        SetActiveAllSys(true);
    }

    public void SetActiveAllSys(bool active)
    {
        for (int i = 0; i < Systems.Count; i++)
        {
            if (Systems[i] is BaseSystem bs)
            {
                bs.IsActive = active;
            }
        }
    }

    public virtual void OnDisable()
    {
        SetActiveAllSys(false);
    }


    protected virtual void OnValidate() { }
    protected virtual void OnDrawGizmos()
    {
        OnGizmosUpdate?.Invoke();
    }
    protected virtual void Awake()
    {
        EntitySetup();
    }

    protected virtual void EntitySetup()
    {
        InitSystems();
    }

    public virtual void Update()
    {
        OnUpdate?.Invoke();
    }
    public virtual void FixedUpdate()
    {
        OnFixedUpdate?.Invoke();
    }

    public virtual void LateUpdate()
    {
        OnLateUpdate?.Invoke();
    }
    protected virtual void InitSystems()
    {
        foreach (var system in Systems)
        {
            system.Initialize(this);
            if(system is BaseSystem bs)
                bs.IsActive = true;
        }
    }

    public void AddControllerComponent<T>(T component) where T : IComponent
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T)
            {
                Components[i] = component;
                return;
            }
        }

        Components.Add(component);
    }

    public T GetControllerComponent<T>() where T :  IComponent
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T match)
                return match;
        }

        return default;
    }


    public void AddControllerSystem<T>(T system) where T : ISystem
    {
        for (int i = 0; i < Systems.Count; i++)
        {
            if (Systems[i] is T)
            {
                Systems[i] = system;
                return;
            }
        }

        Systems.Add(system);
    }

    public T GetControllerSystem<T>() where T : class, ISystem
    {
        for (int i = 0; i < Systems.Count; i++)
        {
            if (Systems[i] is T match)
                return match;
        }

        return null;
    }
    protected virtual void ReferenceClean()
    {

    }

    public void DisposeSystems()
    {
        foreach (var sys in Systems)
        {
            if (sys is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public virtual void OnDestroy()
    {
        DisposeSystems();

        ReferenceClean();
    }
}