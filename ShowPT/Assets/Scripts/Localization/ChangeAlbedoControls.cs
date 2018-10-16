﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeAlbedoControls : MonoBehaviour
{

    public Texture2D[] emisionTextures;
    public Material material;
    // Use this for initialization
    void Start ()
	{
	    Texture2D texture;
	    switch (LocalizationManager.instance.getLenguage())
	    {
            case "EN.json":
                texture = emisionTextures[0];
                break;
	        case "ES.json":
	            texture = emisionTextures[1];
                break;
	        case "AR.json":
	            texture = emisionTextures[2];
                break;
	        case "PT.json":
	            texture = emisionTextures[3];
                break;
	        case "DE.json":
	            texture = emisionTextures[4];
                break;
	        case "FR.json":
	            texture = emisionTextures[5];
                break;
	        default:
	            texture = emisionTextures[0];
                break;
        }
	    material.SetTexture("_MainTex", texture);
    }
	
}