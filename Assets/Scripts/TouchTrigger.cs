// using UnityEngine;

// public class TouchTrigger : MonoBehaviour
// {
//     [SerializeField]
//     private CodeController controller;

//     private void OnTriggerEnter2D(
//         Collider2D other)
//     {
//         Entity target =
//             other.GetComponent<Entity>();

//         if (target == null)
//             return;

//         controller.Execute(target);
//     }
// }