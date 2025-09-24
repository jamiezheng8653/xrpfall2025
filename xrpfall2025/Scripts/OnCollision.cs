using System;

public sealed class OnCollision
{
    public delegate void CollisionEventHandler(object sender, OnCollisionEventArgs e);
    public static event CollisionEventHandler OnCollisionDetected;

    public static void UpdateState(Object obj, OnCollisionEventArgs e)
    {
        if (OnCollisionDetected != null)
        {
            OnCollisionDetected(obj, e);
        }

    }

}


public class OnCollisionEventArgs : EventArgs
{
    private Selection itemSelected = 0;
    public OnCollisionEventArgs(Selection itemSelected)
    {
        this.itemSelected = itemSelected;
    }
}