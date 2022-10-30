using UnityEngine;

//the create paradigm is not good like the MonoModule because you can't pass 
//anything to create, unlike the mono objects in the scene
public abstract class Singleton<T> where T : Singleton<T>
{
    public static T I;

    protected Singleton()
    {
        // if (I != null)
        // throw new SingletonException();
        //since I restart the game on some occasions, I may rewrite the singleton

        I = (T)this;
        Debug.Log("base constructor is called");
    }
}

public class SingletonException : System.Exception
{
    public SingletonException() : base("you are trying to make more than one singleton")
    {
    }
}