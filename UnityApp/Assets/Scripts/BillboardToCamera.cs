using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class BillboardToCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        // Koristimo direktnu rotaciju kamere umjesto računanja smjera.
        // Ovo osigurava da je panel uvijek savršeno paralelan sa ekranom.
        transform.rotation = Camera.main.transform.rotation;
        
        // Ako ipak želiš da panel bude samo vertikalan (kao u tvom kodu), 
        // koristi ovo umjesto gornje linije, ali bez dir.y = 0:
        /*
        Vector3 lookPos = Camera.main.transform.position - transform.position;
        lookPos.y = 0; // Zadržava panel uspravnim
        if (lookPos != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(-lookPos);
            transform.rotation = rotation;
        }
        */
    }
}
