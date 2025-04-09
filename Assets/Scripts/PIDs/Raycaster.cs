using UnityEngine;

public class Raycaster : MonoBehaviour
{
    public Selectable targetScript;
    public GameObject target;
    public GameObject aimer;
    public void Start()
    {

    }

    public void Update()
    {
        Debug.DrawRay(transform.position, transform.up * 3f, Color.green);
        Debug.DrawRay(transform.position, transform.right * 3f, Color.red);
        Debug.DrawRay(transform.position, transform.forward * 3f, Color.blue);

        Vector3 castDir = transform.forward;
        Debug.DrawRay(transform.position, castDir*1000f, Color.black);
        if (Physics.Raycast(transform.position, castDir, out RaycastHit hitInfo, Mathf.Infinity))
        {
            target = hitInfo.collider.gameObject;
            targetScript = target.GetComponent<Selectable>();
            Debug.Log("HIT: " + hitInfo.collider.name + hitInfo.collider.gameObject.tag);
            Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.white);
            aimer.transform.position = hitInfo.point + Vector3.up;

            if(target.tag == "Selectable") { /*do work*/ }
        }
        else { target = null; targetScript = null; }
    }

}
