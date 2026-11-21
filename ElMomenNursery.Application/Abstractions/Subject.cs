namespace ElMomenNursery.Application.Abstractions;

public abstract class Subject
{
    private List<Observer> observers = new List<Observer>();

    public abstract string GetState();
    public abstract void SetState(string state, Guid childId);

    public void Attach(Observer observer)
    {
        observers.Add(observer);
    }

    public void Detach(Observer observer)
    {
        observers.Remove(observer);
    }

    public void Notify(Guid childId)
    {
        foreach (var observer in observers)
        {
            observer.Update(childId);
        }
    }
}
