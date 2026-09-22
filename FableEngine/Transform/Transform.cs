using FableEngine.Math;
using FableEngine.Core;
using FableEngine.Math;

namespace FableEngine.Transforms;

public interface ITransformProvider : IDestroyable, INameable
{
    public Transform transform { get; }
    public Transform? parent => transform.parent;
    public T Transform<T>() where T : Transform => (T)transform;
}


// Transform class on its own can be used to have tree hierarchy without having any dimensions too,
// alongside the ability to derive from it to create any dimensional transform classes such as 1D, 2D, 3D, 4D, 5D, etc.
public class Transform : IDestroyable
{
    /// <summary>
    /// A List of unparented transforms, floating in the void
    /// </summary>
    public static List<Transform> roots { get; private set; } = [];

    public virtual IVector Position { get; set; }

    protected internal Matrix4x4 matrix
    {
        get
        {
            if (cached || matrixOverride) return field;
            field = GetLocalTransformMatrix();
            if (parent != null) field = parent.matrix * field;
            cached = true;
            return field;
        }
        set;
    } = new();
    protected internal bool matrixOverride = false;
    public List<Transform> children { get; private set; } = [];
    public int totalChildren
    {
        get;
        internal set
        {
            parent?.totalChildren += value - field;
            field = value;
        }
    } = 0;
    protected internal bool cached
    {
        get;
        set
        {
            field = value;
            if (!field)
            {
                worldToLocalCache = null;
                foreach (Transform child in children)
                    child.cached = false;
            }
        }
    } = false;
    public int depth { get; private set; } = 0;
    
    [ShowDeclared]
    public Transform? parent
    {
        get;
        set
        {
            if (field == value) return;
            parentVersion++;
            if (field != null)
            {
                field.children.Remove(this);
                field.totalChildren-=totalChildren+1;
            }
            field = value;
            if (field != null)
            {
                field.children.Add(this);
                field.totalChildren+=totalChildren+1;
                // todo: add ability to keep world transform
                roots.Remove(this);
                depth = field.depth+1;
            }
            else
            {
                // todo: add ability to keep world transform
                roots.Add(this);
                depth = 0;
            }
        }
    }
    /// <summary>
    /// Get the root parent of this transform, which is the upmost parent that's parent is null.
    /// </summary>
    public Transform root
    {
        get
        {
            Transform T = this;
            while (T.parent != null)
                T = T.parent;
            return T;
        }
    }
    /// <summary>
    /// This number increases each time parent of this transform changes. It can be used to track when a change in hierarchy happens.
    /// </summary>
    public int parentVersion { get;
        private set {
            foreach (Transform child in children)
                child.parentVersion += 1;
            field = value;
        }
    }

    protected internal ITransformProvider attachedObject;

    public Transform(ITransformProvider provider, Transform? parent = null)
    {
        // set provider
        attachedObject = provider;
        this.parent = parent;
    }

    protected Matrix4x4? worldToLocalCache;
    public Matrix4x4 worldToLocal
    {
        get
        {
            if (worldToLocalCache != null) return (Matrix4x4)worldToLocalCache;
            worldToLocalCache = matrix.Inverse();
            return (Matrix4x4)worldToLocalCache;
        }
        set => worldToLocalCache = value;
    }
    public Matrix4x4 localToWorld => matrix;


    public void OverrideMatrix(Matrix4x4 newMatrix)
    {
        matrixOverride = true;
        matrix = newMatrix;
        worldToLocalCache = null;
        cached = false;
    }

    public void ReleaseOverride()
    {
        matrixOverride = false;
        worldToLocalCache = null;
        cached = false;
    }
    
    public void SwapChildren(int indexA, int indexB)
    {
        (children[indexA], children[indexB]) = (children[indexB], children[indexA]);
    }

    public Transform? GetChild(int i)
    {
        if (i < 0 || i > children.Count) return null;
        return children[i];
    }

    public bool IsChildOf(Transform transform)
    {
        return parent == transform;
    }

    public bool destroyed { get; private set; }

    public void Destroy()
    {
        if (destroyed) return;
        destroyed = true;
        if (!attachedObject.destroyed) attachedObject.Destroy();
        foreach (Transform child in children.ToArray())
        {
            child.Destroy();
        }
        parent = null;
        children.Clear();
        roots.Remove(this);
        disposed = true;
    }
    private bool disposed = false;
    public static bool operator !(Transform t) => t.disposed;

    public T Cast<T>() where T : Transform => (this as T) ?? throw new InvalidOperationException();
    
    /// <summary>
    /// Method used to calculate local transform matrix from internal values such as position, rotation, scale, etc.
    /// </summary>
    /// <returns>local transform matrix, not including matrix override</returns>
    public virtual Matrix4x4 GetLocalTransformMatrix() => Matrix4x4.identity;
}