namespace Systems
{
    public interface ICopyable
    {
        IComponent Copy();
    }

    public interface ReInitAfterRePlay
    {
        public void ReInit();
    }
}