using System.Collections.Generic;

public static class ListPool<T>
{
    private static Stack<List<T>> stack = new Stack<List<T>>();

    public static List<T> Get() {
        return stack.Count > 0 ? stack.Pop() : new List<T>();
    }

    public static void Add(List<T> list) {
        list.Clear();
        stack.Push(list);
    }
}