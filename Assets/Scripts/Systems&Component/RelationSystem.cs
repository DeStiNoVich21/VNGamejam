using Sirenix.OdinInspector;
using Systems;

public class RelationSystem : BaseSystem
{
    public RelationComponent relationComponent;
    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        relationComponent = owner.GetControllerComponent<RelationComponent>();
    }
    public void PlusLove(float love)
    {
        relationComponent.love += love;
    }
    public void PlusRep(float rep)
    {
        relationComponent.reputation += rep;
    }
}


[System.Serializable]
public class RelationComponent : IComponent
{
    [PropertyRange(-100,100)]
    public float reputation = 0;
    [PropertyRange(-100, 100)]
    public float love = 0;
}
