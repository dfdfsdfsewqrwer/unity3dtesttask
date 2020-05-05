using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject plane;

    public GameObject obstaclePrefab;
    public int obstacleMinSize;
    public int obstacleMaxSize;
    public float safeTop;
    public float safeBottom;

    public GameObject car;
    public GameObject carHighlight;
    public float carBottom;
    public float carAcceleration;
    public float carMaxSpeed;
    public float carRotationSpeed;
    public float carFreedom;
    public int carHitPoints;
    public float carWheelFrictionFrom;
    public float carWheelFrictionTo;
    public float carWheelFrictionMaxAngle;
    public float carGameOverTimeout;

    public GameObject star;
    public GameObject starHighlight;
    public float starTop;

    public GameObject brush;
    public float brushFrequency;

    // private stuff
    private bool isCarHovered = false;
    private bool isCarClicked = false;
    private bool isStarHovered = false;
    private Vector2 lastBrushPos = new Vector2();
    private List<GameObject> brushes = new List<GameObject>();
    private GameObject targetBrush;
    private int carhp;
    private float cartimeout;
    private int level = 0;

    enum GamePhase
    {
        Idle,
        Busy,
        Drawing,
        Moving,
    };
    GamePhase gamePhase = GamePhase.Idle;

    public void NewGame(bool next_level)
    {
        gamePhase = GamePhase.Busy;

        // stop, reset and cleanup
        var rigidbody = car.GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        car.transform.rotation = Quaternion.identity;
        isCarClicked = false;
        isCarHovered = false;
        isStarHovered = false;
        ClearBrushes();

        Bounds planebounds = plane.GetComponent<Renderer>().bounds;


        if (next_level)
        {
            level++;

            // clear area
            GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Obstacle");
            foreach (GameObject obj in allObjects)
                Destroy(obj);

            // generate obstacles
            for (int i = 0; i < level; i++)
            {
                int xsize, zsize;
                if (Random.Range(1, 3) == 1)
                {
                    xsize = Random.Range(obstacleMinSize, obstacleMaxSize);
                    zsize = 1;
                }
                else
                {
                    xsize = 1;
                    zsize = Random.Range(obstacleMinSize, obstacleMaxSize);
                }

                float xpos = Mathf.RoundToInt(Random.Range(planebounds.min.x, planebounds.max.x - xsize) + 0.5f);
                float zpos = Mathf.RoundToInt(Random.Range(planebounds.min.z + safeBottom, planebounds.max.z - safeTop - zsize) + 0.5f);

                for (int xi = 0; xi < xsize; xi++)
                    for (int zi = 0; zi < zsize; zi++)
                    {
                        var obstacle = Instantiate(obstaclePrefab.gameObject, new Vector3(xpos + xi, 0.5f, zpos + zi), Quaternion.identity);
                        obstacle.GetComponent<Obstacle>().gameManager = this;
                    }

            }
        }

        // init car
        {
            Vector3 v = new Vector3(Random.Range(planebounds.min.x + 1, planebounds.max.x - 1), 0.5f, planebounds.min.z + carBottom);
            car.transform.position = v;
            v.y = 0.01f;
            carHighlight.transform.position = v;
            carHighlight.SetActive(false);

            // reset brush to car position
            lastBrushPos.x = v.x;
            lastBrushPos.y = v.z;

            carhp = carHitPoints;
        }

        // init exit point
        {
            Vector3 v = new Vector3(Random.Range(planebounds.min.x + 1, planebounds.max.x - 1), 0, planebounds.max.z - starTop);
            star.transform.position = v;
            v.y = 0.01f;
            starHighlight.transform.position = v;
            starHighlight.SetActive(false);
        }

        gamePhase = GamePhase.Idle;
    }

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);


        NewGame(true);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            // start new game
            level = 0;
            NewGame(true);
            return;
        }

        var phase = gamePhase;

        if (phase == GamePhase.Drawing)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if ( Physics.Raycast(ray, out hit)) {
                Vector2 brushPos = new Vector2(hit.point.x, hit.point.z);

                if (Vector2.Distance(brushPos, lastBrushPos) >= brushFrequency)
                {
                    GameObject brushInstance = Instantiate(brush, new Vector3(hit.point.x, 0.01f, hit.point.z), Quaternion.identity, transform);
                    brushes.Add(brushInstance);
                    lastBrushPos = brushPos;
                }

            }
        }
        else if (phase == GamePhase.Moving)
        {
            var distanceToTarget = Vector2.Distance(new Vector2(car.transform.position.x, car.transform.position.z), new Vector2(targetBrush.transform.position.x, targetBrush.transform.position.z));
            if ( distanceToTarget < carFreedom )
                MoveToNextBrush();

            cartimeout -= Time.deltaTime;
            if (cartimeout < 0)
            {
                // car stuck
                GameOver();
                return;
            }

            if (
                // if car is in the air or underground...
                car.transform.position.y < carWheelFrictionFrom ||
                car.transform.position.y > carWheelFrictionTo ||

                // or if it's not horizontal enough...
                Mathf.Abs( car.transform.rotation.x ) > carWheelFrictionMaxAngle ||
                Mathf.Abs( car.transform.rotation.z ) > carWheelFrictionMaxAngle
            )
            {
                // ...then it doesn't have enough friction and can't accelerate or turn
                return;
            }

            float accel = carAcceleration;
            float rotateSpeed = carRotationSpeed;

            // get relative angle between car and target to determine direction of turning
            Vector3 targetDir = targetBrush.transform.position - car.transform.position;
            float angle = Vector3.Angle(targetDir, car.transform.forward);
            if (Vector3.Angle(car.transform.right, targetDir) > 90f)
                angle *= -1;
            int rotateDir = angle > 0 ? 1 : -1;


            // reduce acceleration based on how far from forward direction is target
            accel -= Mathf.Abs(angle)/90*accel;

            //if (Mathf.Abs(angle)<5)
                //rotateSpeed = 0;

            // rotate towards next target
            car.transform.RotateAround(car.transform.position, car.transform.up, Time.deltaTime * rotateSpeed * rotateDir);

            if (car.GetComponent<Rigidbody>().velocity.magnitude < carMaxSpeed)
            {
                // add some randomness
                accel += accel * Random.Range(-0.2f, 0.2f);

                // accelerate
                car.GetComponent<Rigidbody>().AddForce(car.transform.forward * Time.deltaTime * accel, ForceMode.Impulse);
            }

        }
    }

    public void OnCarMouseOver()
    {
        if (gamePhase != GamePhase.Idle)
            return;
        isCarHovered = true;
        if (!isCarClicked)
            carHighlight.SetActive(true);
    }

    public void OnCarMouseExit()
    {
        isCarHovered = false;
        if (!isCarClicked)
            carHighlight.SetActive(false);
    }

    public void OnCarMouseDown()
    {
        if (gamePhase != GamePhase.Idle)
            return;
        isCarClicked = true;
        if ( !isCarHovered )
            carHighlight.SetActive(true);
        gamePhase = GamePhase.Drawing;
    }

    private void ClearBrushes()
    {
        // clear brushes
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Brush");
        foreach (GameObject obj in allObjects)
            Destroy(obj);
        brushes.Clear();
    }

    public void OnCarMouseUp()
    {
        if (gamePhase != GamePhase.Drawing)
            return;
        isCarClicked = false;
        if ( !isCarHovered )
            carHighlight.SetActive(false);

        if (isStarHovered)
        {
            // drive car using drawn trajectory
            MoveToNextBrush();
            gamePhase = GamePhase.Moving;
        }
        else
        {
            ClearBrushes();
            gamePhase = GamePhase.Idle;
        }
    }

    public void OnStarMouseOver()
    {
        if (gamePhase != GamePhase.Drawing)
            return;
        isStarHovered = true;
        starHighlight.SetActive(true);
    }

    public void OnStarMouseExit()
    {
        if (gamePhase != GamePhase.Drawing)
            return;
        isStarHovered = false;
        starHighlight.SetActive(false);
    }

    private void MoveToNextBrush()
    {
        cartimeout = carGameOverTimeout;
        targetBrush = brushes[0];
        if (brushes.Count > 1) // never remove last target
            brushes.RemoveAt(0);
    }

    public void Crash()
    {
        carhp--;
        
        if (carhp<=0)
            GameOver();
    }

    public void GameOver()
    {
        if (gamePhase == GamePhase.Moving)
        {
            // ... do something ...

            NewGame(false);
        }
    }

}
