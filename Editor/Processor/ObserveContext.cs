using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor.Processor;

internal interface IObserveContext
{
    public T Observe<T>(T obj) where T : Object;
    public R Observe<T, R>(T obj, Func<T, R> extract, Func<R, R, bool>? compare = null) where T : Object;
    public bool EditorOnlyInHierarchy(GameObject obj);
    public void GetComponents<C>(GameObject obj, List<C> results) where C : Component;
    public void GetComponentsInChildren<C>(GameObject obj, bool includeInactive, List<C> results) where C : Component;
}

internal class NonObserveContext : IObserveContext
{
    public T Observe<T>(T obj) where T : Object
    {
        return obj;
    }

    public R Observe<T, R>(T obj, Func<T, R> extract, Func<R, R, bool>? compare = null) where T : Object
    {
        return extract(obj);
    }

    public bool EditorOnlyInHierarchy(GameObject obj)
    {
        const string editorOnlyTag = "EditorOnly";
        var current = obj;
        while (current != null)
        {
            if (current.CompareTag(editorOnlyTag))
            {
                return true;
            }
            var parent = current.transform.parent;
            current = parent != null ? parent.gameObject : null;
        }
        return false;
    }

    public void GetComponents<C>(GameObject obj, List<C> results) where C : Component
    {
        obj.GetComponents(results);
    }

    public void GetComponentsInChildren<C>(GameObject obj, bool includeInactive, List<C> results) where C : Component
    {
        obj.GetComponentsInChildren(includeInactive, results);
    }
}

internal class NDMFObserveContext : IObserveContext
{
    private readonly ComputeContext _context;

    public NDMFObserveContext(ComputeContext context)
    {
        _context = context;
    }

    public T Observe<T>(T obj) where T : Object
    {
        _context.Observe(obj);
        return obj;
    }

    public R Observe<T, R>(T obj, Func<T, R> extract, Func<R, R, bool>? compare = null) where T : Object
    {
        return _context.Observe(obj, extract, compare);
    }

    public bool EditorOnlyInHierarchy(GameObject obj)
    {
        const string editorOnlyTag = "EditorOnly";
        var current = obj;
        while (current != null)
        {
            var isEditorOnly = _context.Observe(current, g => g.CompareTag(editorOnlyTag), (a, b) => a == b);
            if (isEditorOnly) return true;
            var parent = _context.Observe(current, g => g.transform.parent, (a, b) => a == b);
            current = parent != null ? parent.gameObject : null;
        }
        return false;
    }

    public void GetComponents<C>(GameObject obj, List<C> results) where C : Component
    {
        _context.GetComponents(obj, results);
    }

    public void GetComponentsInChildren<C>(GameObject obj, bool includeInactive, List<C> results) where C : Component
    {
        _context.GetComponentsInChildren(obj, includeInactive, results);
    }
}
