﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kamikaze : Enemy {

    Transform player;
    public float explosionDistance = 20f;
    public int explosionDamage = 3;

	[SerializeField]
	GameObject explosion;

    // Use this for initialization
    void Start() 
	{
		player = GameObject.FindGameObjectWithTag ("Player").GetComponent<Transform>();
	    ctrAudio = GameObject.FindGameObjectWithTag("CtrlAudio").GetComponent<CtrlAudio>();
        cameraShake = GameObject.FindGameObjectWithTag("CameraShake").GetComponent<CameraShake>();
    }

    // Update is called once per frame
    void Update()
    {
        //hasExplode();
    }

    public override void getHit(int damage)
    {
        ctrAudio.playOneSound("Enemies", hitAudio, transform.position, 1.0f, 0.0f, 128);
        //hitAudio = ctrAudio.hit;
        enemyHealth -= damage;
        //Debug.Log(enemyHealth);
        /*if (enemyHealth <= 0)
        {
            forceExplode();
        }*/
        checkHealth();
    }

    private void forceExplode()
    {
        //Explosion animation
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, player.position - transform.position, out hitInfo, explosionDistance) && hitInfo.transform.tag == "Player")
        {
            player.GetComponent<PlayerHealth>().ChangeHealth(explosionDamage);
        }
        GameObject.Instantiate(explosion, transform.position, Quaternion.identity);

        //Camera Shake
        float playerDistance = Vector3.Distance(transform.position, player.position);
        cameraShake.startShake(shakeTime, fadeInTime, fadeOutTime, speed, (magnitude * (1 - Mathf.Clamp01(playerDistance / maxDistancePlayer))));

        generateDeathEffect ();
    }

    private void hasExplode()
    {
        if (player != null && Vector3.Distance(player.position, transform.position) <= explosionDistance)
        {
            //Explosion animation
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, player.position - transform.position, out hitInfo, explosionDistance) && hitInfo.transform.tag == "Player")
            {
                player.GetComponent<PlayerHealth>().ChangeHealth(explosionDamage);
            }
			GameObject.Instantiate (explosion, transform.position, Quaternion.identity);
			generateDeathEffect ();
            //Destroy(gameObject);
        }
    }

    public void explode()
    {
       // if (player != null && Vector3.Distance(player.position, transform.position) <= explosionDistance)
       // {
            forceExplode();
       // }
    }

    public override void checkHealth()
    {
        if (enemyHealth <= 0f)
        {
            forceExplode();
            ScoreController.addDead(ScoreController.Enemy.KAMIKAZE);
        }
    }
}
