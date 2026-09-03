using UnityEngine;

public class CodeGridVisual : MonoBehaviour
{
    [SerializeField] private CodeGrid grid;
    [SerializeField] private Color lineColor = Color.gray;

    // private void OnDrawGizmos()
    // {
    //     if (grid == null)
    //         return;

    //     Gizmos.color = lineColor;

    //     float cellSize = grid.CellSize;

    //     for (int x = 0; x <= grid.Width; x++)
    //     {
    //         Vector3 start = transform.TransformPoint(
    //             new Vector3(x * cellSize, 0f, 0f)
    //         );

    //         Vector3 end = transform.TransformPoint(
    //             new Vector3(x * cellSize, grid.Height * cellSize, 0f)
    //         );

    //         Gizmos.DrawLine(start, end);
    //     }

    //     for (int y = 0; y <= grid.Height; y++)
    //     {
    //         Vector3 start = transform.TransformPoint(
    //             new Vector3(0f, y * cellSize, 0f)
    //         );

    //         Vector3 end = transform.TransformPoint(
    //             new Vector3(grid.Width * cellSize, y * cellSize, 0f)
    //         );

    //         Gizmos.DrawLine(start, end);
    //     }
    // }
}