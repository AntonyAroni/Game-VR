using UnityEngine;

namespace ZombieCheckpoint.Core
{
    /// <summary>
    /// Utilidades de componentes seguras frente al "fake null" de Unity.
    /// En el Editor, GetComponent devuelve un objeto nulo falso cuando el componente no existe;
    /// el operador ?? de C# no lo detecta, así que el patrón GetComponent() ?? AddComponent()
    /// nunca llegaba a agregar el componente.
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        /// Devuelve el componente <typeparamref name="T"/> del GameObject o lo agrega si no está presente.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return go.TryGetComponent(out T existing) ? existing : go.AddComponent<T>();
        }
    }
}
