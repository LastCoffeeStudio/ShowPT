﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Scene data")]
    public GameObject minLimit;
    public GameObject maxLimit;

    [Header("Boss components")]
    public Arm[] arms;
    public GameObject platform1;
    public GameObject platform2;
    public GameObject body;
    public GameObject bodyPivot;
    public GameObject head;
    public GameObject spawnSelector;
    public GameObject arrow;

    [Header("Boss attributes")]
    public int idleDamage;
    public int rotationDamage;
    public int rollDamage;

    [Header("Rotate attack attributes")]
    public int totalRepeatNumber;
    public int rollRepeatNumber;
    public float maxSpeed;
    public float acceleration;
    public float timeToRoll;
    public float minPlayerDistance;
    public AnimationClip rotateAnim;
    public AnimationCurve rollDisplacementCurve;

    [Header("Arms attack attributes")]
    public float recoverArmsStartTime;

    [Header("Roulette attack attributes")]
    public EnemySpawn[] enemies;
    //TODO: change spawnpoints for an area when the boss room is flat.
    public GameObject[] points;
    
    private GameObject player;
    private Bounds combatZone;
    private Animator bossAnimator;
    private Vector3 playerPosition;
    private STATES state;
    private int rollRepeat;
    private int totalRepeat;
    private float rollDirectionX;
    private float rollDirectionZ;
    private int armsStopped;
    private int armsDead;
    private int rouletteRoll;
    private float chaseSpeed;
    private float timeElapsed;
    private float sideLength;
    private bool armsActive;
    private bool chasePlayer;
    private bool[] spawns;

    private enum STATES
    {
        ROTATE_ATTACK,
        ARMS_ATTACK,
        ROULETTE_ATTACK,
        DEFEAT,
        END_STATE
    }

    [System.Serializable]
    public struct Arm
    {
        [HideInInspector]
        public int id;
        public GameObject arm;
        public int health;
        public int damage;
        public float attackDistance;
        public float speed;
        [HideInInspector]
        public bool dead;
        [HideInInspector]
        public bool stopped;
        [HideInInspector]
        public float delayLaunchTime;
        [HideInInspector]
        public Vector3 initialPosition;
        [HideInInspector]
        public Vector3 endingPosition;
    }

    [System.Serializable]
    public struct EnemySpawn
    {
        public Enemy.EnemyType type;
        public GameObject enemy;
        public int minNumEnemies;
        public int maxNumEnemies;
    }
    
    
    /**Boss**/

	void Start ()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        bossAnimator = GetComponent<Animator>();
        Vector3 combatZoneSize = (minLimit.transform.position - maxLimit.transform.position);
        combatZoneSize = new Vector3(Mathf.Abs(combatZoneSize.x), Mathf.Abs(combatZoneSize.y), Mathf.Abs(combatZoneSize.z));
        combatZone = new Bounds((maxLimit.transform.position - minLimit.transform.position) / 2f + minLimit.transform.position, combatZoneSize);
        spawns = new bool[points.Length];
        setArms();
        createRotationCurve();
        initializeVars();
    }
	
	void Update ()
    {
        switch (state)
        {
            case STATES.ROTATE_ATTACK:

                if (chasePlayer)
                {
                    if (Vector3.Distance(transform.position, playerPosition) >= minPlayerDistance && combatZone.Contains(transform.position))
                    {
                        chaseSpeed += acceleration * Time.deltaTime;
                        chaseSpeed = Mathf.Clamp(chaseSpeed, 0, maxSpeed);
                        Vector3 dir = (playerPosition - transform.position).normalized;
                        dir.y = 0f;
                        Debug.DrawRay(transform.position, (playerPosition - transform.position), new Color(0f, 1f, 0f, 1f));
                        transform.Translate(dir * chaseSpeed * Time.deltaTime, Space.World);
                    }
                    else
                    {
                        chaseSpeed = 0f;
                        chasePlayer = false;
                        bossAnimator.SetBool("PlayerCaught", true);

                        //SOB stop search player y
                    }
                }
                if (bossAnimator.GetBool("Roll") && !bossAnimator.GetBool("Rolling"))
                {
                    bossAnimator.SetBool("Rolling", true);
                    calculateRollData();
                }
                break;
            case STATES.ARMS_ATTACK:
                if (armsActive)
                {
                    if (armsDead == arms.Length)
                    {
                        bossAnimator.SetInteger("State", 3);
                        state = STATES.DEFEAT;
                        break;
                    }

                    timeElapsed += Time.deltaTime;
                    if (armsStopped < (arms.Length - armsDead))
                    {
                        for (int i = 0; i < arms.Length; ++i)
                        {
                            if (!arms[i].dead && !arms[i].stopped && arms[i].delayLaunchTime <= timeElapsed)
                            {
                                //SOB start punch to hit ADD CONDITION ONLY ONE
                                float displacement = arms[i].speed * Time.deltaTime;
                                arms[i].arm.transform.Translate(arms[i].arm.transform.forward * displacement, Space.World);

                                if (Vector3.Distance(arms[i].initialPosition, arms[i].arm.transform.position) >= arms[i].attackDistance)
                                {
                                    stopArm(i);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (timeElapsed <= recoverArmsStartTime)
                        {
                            for (int i = 0; i < arms.Length; ++i)
                            {
                                if (!arms[i].dead)
                                {
                                    float displacement = arms[i].speed * Time.deltaTime;
                                    arms[i].arm.transform.position = Vector3.Slerp(arms[i].endingPosition, arms[i].initialPosition, timeElapsed / recoverArmsStartTime);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < arms.Length; ++i)
                            {
                                if (!arms[i].dead)
                                {
                                    arms[i].arm.transform.position = arms[i].initialPosition;
                                    arms[i].arm.GetComponent<BossArmController>().vulnerable = false;
                                }
                            }
                            timeElapsed = 0f;
                            armsStopped = 0;
                            bossAnimator.SetInteger("State", 2);
                            state = STATES.ROULETTE_ATTACK;
                            armsActive = false;
                        }
                    }
                }
                break;
            default:
                break;
        }
	}

    public int getBossDamage(int armId = -1)
    {
        switch (state)
        {
            case STATES.ROTATE_ATTACK:
                if (bossAnimator.GetBool("Roll"))
                {
                    return rollDamage;
                }
                else
                {
                    return rotationDamage;
                }
            case STATES.ARMS_ATTACK:
                if (armId != -1)
                {
                    return arms[armId].damage;
                }
                break;
        }

        return idleDamage;
    }

    public void stopArm(int id)
    {
        arms[id].stopped = true;
        arms[id].endingPosition = arms[id].arm.transform.position;
        ++armsStopped;
        //SOB Punch hit wall
        if (armsStopped == (arms.Length - armsDead))
        {
            timeElapsed = 0f;
            //SOB punch come back
        }
    }

    public float getHitArm(int id, int damage)
    {
        arms[id].health -= damage;
        Debug.Log(arms[id].health);
        if (arms[id].health <= 0)
        {
            arms[id].dead = true;
            ++armsDead;
            arms[id].arm.SetActive(false);
            //Activate death effect (activate another "Arm" broken, stays on the ground)
        }

        return arms[id].health;
    }

    
    /**Animation calls**/

    public void startChasingPlayer()
    {
        playerPosition = player.transform.position;
        chasePlayer = true;
        bossAnimator.SetBool("PlayerCaught", false);
        //SOB moverse rotandose en Y al jugador
    }

    public void calculateRollData()
    {
        calculateDirectionFromPlayerPosition();
        checkRoll();
        StartCoroutine("rollBoss");
    }

    public void setRoll()
    {
        bossAnimator.SetBool("Rolling", false);
        ++rollRepeat;
        if (rollRepeat >= rollRepeatNumber)
        {
            rollRepeat = 0;
            bossAnimator.SetBool("Roll", false);
            
            transform.position += body.transform.localPosition;
            body.transform.localPosition = Vector3.zero;

            if (totalRepeat >= totalRepeatNumber)
            {
                totalRepeat = 0;
                bossAnimator.SetInteger("State", 1);
                state = STATES.ARMS_ATTACK;
            }
        }
    }

    public void prepareArms(AnimationEvent animationEvent)
    {
        int boolParam = animationEvent.intParameter;

        for (int i = 0; i < arms.Length; ++i)
        {
            if (!arms[i].dead)
            {
                //SOB open each door
                arms[i].arm.SetActive(boolParam != 0);
                arms[i].initialPosition = arms[i].arm.transform.position;
                arms[i].stopped = false;
            }
        }
    }

    public void activateArms()
    {
        armsActive = true;
        for (int i = 0; i < arms.Length; ++i)
        {
            if (!arms[i].dead)
            {
                arms[i].arm.GetComponent<BossArmController>().vulnerable = true;
            }
        }
    }

    public void activatePlatform(AnimationEvent animationEvent)
    {
        int boolParam = animationEvent.intParameter;
        platform1.SetActive(boolParam != 0);
        platform2.SetActive(boolParam != 0);

        if (boolParam != 0)
        {
            ++totalRepeat;
            //SOB activar plataforma
        }
        else
        {
            bossAnimator.SetBool("Roll", true);
            //SOB quitar plataforma
        }
    }

    public void activateRoulette(AnimationEvent animationEvent)
    {
        int boolParam = animationEvent.intParameter;
        head.SetActive(boolParam != 0);
        spawnSelector.SetActive(boolParam != 0);
        arrow.SetActive(boolParam != 0);

        if (boolParam != 0)
        {
            //SOB Show roulete
            rouletteRoll = Random.Range(1,5);
        }
        else
        {
            state = STATES.ROTATE_ATTACK;
            bossAnimator.SetInteger("State",0);
        }
    }

    public void summonEnemies(AnimationEvent animationEvent)
    {
        Enemy.EnemyType type = Enemy.EnemyType.NONE;
        switch (rouletteRoll)
        {
            case 1:
                type = Enemy.EnemyType.LILONE;
                break;
            case 2:
                type = Enemy.EnemyType.TURRET;
                break;
            case 3:
                type = Enemy.EnemyType.KAMIKAZE;
                break;
            case 4:
                type = Enemy.EnemyType.ALL;
                break;
        }
        
        for (int i = 0; i < spawns.Length; ++i)
        {
            spawns[i] = false;
        }

        EnemySpawn enemySpawn = findEnemySpawn(type);
        int numberSpawns = Random.Range(enemySpawn.minNumEnemies, enemySpawn.maxNumEnemies + 1);

        if (enemySpawn.type == Enemy.EnemyType.ALL)
        {
            for (int i = 0; i < numberSpawns; ++i)
            {
                enemySpawn = findRandomEnemySpawn();
                spawnEnemy(enemySpawn);
            }

        }
        else if (enemySpawn.type != Enemy.EnemyType.NONE)
        {
            for (int i = 0; i < numberSpawns; ++i)
            {
                spawnEnemy(enemySpawn);
            }
        }
    }

    private void calculateDirectionFromPlayerPosition()
    {
        //Correct the main transform
        transform.position += body.transform.localPosition;
        body.transform.localPosition = Vector3.zero;

        float angle = Vector3.Angle(transform.forward, (player.transform.position - transform.position).normalized);
        //Negative left, positive right
        Vector3 playerVec = (player.transform.position - transform.position);
        playerVec.y = 0;
        float dot = Vector3.Dot(transform.right, playerVec.normalized);
        rollDirectionZ = 1f;
        rollDirectionX = 0f;

        Debug.Log("--- Direction ---");
        Debug.Log(rollDirectionX + " " + rollDirectionZ);

        if (dot >= 0)
        {
            if (angle > 45 && angle <= 135)
            {
                rollDirectionZ = 0f;
                rollDirectionX = 1f;
                
                //transform.Rotate(0f, 90f, 0f);
            }
            else if (angle > 135)
            {
                rollDirectionZ = -1f;

                //transform.Rotate(0f, 180f, 0f);
            }
        }
        else
        {
            if (angle > 45 && angle <= 135)
            {
                rollDirectionZ = 0f;
                rollDirectionX = -1f;
                    
                //transform.Rotate(0f, -90f, 0f);
            }
            else if (angle > 135)
            {
                rollDirectionZ = -1f;

                //transform.Rotate(0f, -180f, 0f);
            }
        }
    }

    private void checkRoll()
    {
        Debug.Log("--- Check Roll ---");
        Debug.Log(rollDirectionX + " " + rollDirectionZ);
        // Here, rollDirectionX xor rollDirectionZ must be 0, and the other 1 or -1
        // (true, due to previous call to calculateDirectionFromPlayerPosition())
        Vector3 destinationPoint = transform.position + (transform.forward * rollDirectionZ + transform.right * rollDirectionX) * sideLength;
        int count = 0;
        if (!combatZone.Contains(destinationPoint))
        {
            float dirAux = rollDirectionX;
            rollDirectionX = rollDirectionZ;
            rollDirectionZ = -dirAux;
            //transform.Rotate(0f, 90f, 0f);
            destinationPoint = transform.position + (transform.forward * rollDirectionZ + transform.right * rollDirectionX) * sideLength;
            ++count;
        }
        Debug.Log(rollDirectionX + " " + rollDirectionZ);
    }

    private IEnumerator rollBoss()
    {
        //SOB Up for rotate
        float time = 0;
        float diagonal = sideLength / Mathf.Sqrt(2f);
        float angleIncrement, displacementX, displacementY, displacementZ;

        Vector3 initialPosition = body.transform.position;
        Quaternion initialRotation = body.transform.rotation;
        body.transform.Rotate(rollDirectionZ * 90f, 0f, -rollDirectionX * 90f, Space.World);
        Quaternion finalRotation = body.transform.rotation;
        body.transform.rotation = initialRotation;
        

        //float lastYPos = body.transform.position.y;

        while (time < timeToRoll)
        {
            time += Time.deltaTime;
            angleIncrement = Mathf.Lerp(0, Mathf.PI / 2f, time / timeToRoll);
            displacementX = rollDirectionX * diagonal * (Mathf.Cos(45f * Mathf.Deg2Rad) - Mathf.Cos(45f * Mathf.Deg2Rad + angleIncrement));
            displacementY = diagonal * (Mathf.Sin(45f * Mathf.Deg2Rad + angleIncrement) - Mathf.Sin(45f * Mathf.Deg2Rad));
            displacementZ = rollDirectionZ * diagonal * (Mathf.Cos(45f * Mathf.Deg2Rad) - Mathf.Cos(45f * Mathf.Deg2Rad + angleIncrement));
            body.transform.position = new Vector3(initialPosition.x + displacementX, initialPosition.y + displacementY, initialPosition.z + displacementZ);
            body.transform.rotation = Quaternion.Lerp(initialRotation, finalRotation, time / timeToRoll);

            yield return new WaitForFixedUpdate();
        }

        //SOB hit floor
        setRoll();

    }

    private EnemySpawn findEnemySpawn(Enemy.EnemyType type)
    {
        for (int i = 0; i < enemies.Length; ++i)
        {
            if (enemies[i].type == type)
            {
                return enemies[i];
            }
        }

        EnemySpawn spawn = new EnemySpawn();
        spawn.type = Enemy.EnemyType.NONE;

        return spawn;
    }

    private EnemySpawn findRandomEnemySpawn()
    {
        int index = Random.Range(0, enemies.Length);
        EnemySpawn spawn = enemies[index];
        while (enemies.Length > 1 && spawn.type == Enemy.EnemyType.ALL)
        {
            index = Random.Range(0, enemies.Length);
            spawn = enemies[index];
        }

        return spawn;
    }

    private void spawnEnemy(EnemySpawn enemySpawn)
    {
        Debug.Log(enemySpawn.type);
        int randomPoint = Random.Range(0, spawns.Length);
        int checkedPoints = 0;
        //Number of spawnpoints must be greater than maximum num enemies of any enemy!
        while (checkedPoints < spawns.Length)
        {
            Bounds pointBounds = new Bounds(points[randomPoint].transform.position, Vector3.one * 1.5f);
            if (!spawns[randomPoint] && !body.GetComponent<BoxCollider>().bounds.Intersects(pointBounds))
            {
                spawns[randomPoint] = true;
                Instantiate(enemySpawn.enemy, points[randomPoint].transform.position, Quaternion.identity);
                checkedPoints = spawns.Length;
            }
            else
            {
                ++checkedPoints;
                ++randomPoint;
                if (randomPoint >= spawns.Length)
                {
                    randomPoint = 0;
                }
            }
        }
    }

    public void startSpinning()
    {
        //SOB
    }

    public void takeOutPlataform()
    {
        //SOB
    }

    public void closingGate()
    {
        //SOB
    }

    public void rouletteEndRoll ()
    {
        //SOB start hidde roulete
    }

    public void startRouletteSpin()
    {
        //SOB start hidde roulete
    }
    /**Initialization**/

    private void setArms()
    {
        float delayTime = 0f;
        for (int i = 0; i < arms.Length; ++i)
        {
            arms[i].id = i;
            arms[i].arm.GetComponent<BossArmController>().id = i;
            arms[i].dead = false;
            arms[i].delayLaunchTime = delayTime;
            delayTime += 0.3f;
        }
    }

    private void createRotationCurve()
    {
        Keyframe[] frames = new Keyframe[(int)(rotateAnim.length * 60f)];
        rotateAnim.SampleAnimation(gameObject, rotateAnim.length);
        float lastPos = body.transform.rotation.x;

        for (int i = 0; i < frames.Length; ++i)
        {
            rotateAnim.SampleAnimation(gameObject, i/60f);
            float rotationX = body.transform.rotation.x;
            
            if (i > 0)
            {
                rotateAnim.SampleAnimation(gameObject, (i - 1) / 60f);
                float lastRotationX = body.transform.rotation.x;
                frames[i] = new Keyframe(i, ((sideLength * rotationX) / lastPos) - ((sideLength * lastRotationX) / lastPos));
            }
            else
            {
                frames[i] = new Keyframe(i, (sideLength * body.transform.rotation.x) / lastPos);
            }
        }
        rollDisplacementCurve = new AnimationCurve(frames);
    }

    private void initializeVars()
    {
        state = STATES.ROTATE_ATTACK;
        rollRepeat = 0;
        totalRepeat = 0;
        rollDirectionX = 0f;
        rollDirectionZ = 0f;
        armsStopped = 0;
        armsDead = 0;
        rouletteRoll = 0;
        chaseSpeed = 0f;
        timeElapsed = 0f;
        sideLength = body.GetComponent<Renderer>().bounds.size.z;
        armsActive = false;
        chasePlayer = false;
    }
    
    /**Utils**/

    void OnDrawGizmosSelected()
    {
        Vector3 bSize = (minLimit.transform.position - maxLimit.transform.position);
        bSize = new Vector3(Mathf.Abs(bSize.x), Mathf.Abs(bSize.y), Mathf.Abs(bSize.z));
        Bounds b = new Bounds((maxLimit.transform.position - minLimit.transform.position) / 2f + minLimit.transform.position, bSize);
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(b.center, b.size);
    }
}
