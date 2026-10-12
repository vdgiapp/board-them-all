#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Gustom.Editor
{
    public sealed class CustomEditorCoroutine
    {
        private readonly Stack<IEnumerator> _stack = new();

        private CustomEditorCoroutine(IEnumerator coroutine)
        {
            if (coroutine != null) _stack.Push(coroutine);
        }

        public static CustomEditorCoroutine Start(IEnumerator coroutine)
        {
            CustomEditorCoroutine customEditorCoroutine = new(coroutine);
            customEditorCoroutine.Start();
            return customEditorCoroutine;
        }

        private void Start()
        {
            EditorApplication.update += Update;
        }

        public void Stop()
        {
            EditorApplication.update -= Update;
            _stack.Clear();
        }

        private void Update()
        {
            if (!MoveNext()) Stop();
        }

        private bool MoveNext()
        {
            while (_stack.Count > 0)
            {
                IEnumerator current = _stack.Peek();
                if (!current.MoveNext())
                {
                    _stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested) _stack.Push(nested);
                return true;
            }
            return false;
        }
    }
}
#endif
