﻿using UnityEngine;
using System.Collections;

public enum Tutorials {none,move,sit,standup,interact,follow};

public class TutorialGui : MonoBehaviour {
	public string[] TutorialStrings = {"",	"Press <b>ArrowKeys</b> to move.",
										  	"Press <b>Q</b> to sit.",
											"Press <b>Q</b> to stand up again.",
											"Press <b>E</b> to interact.",
											"Follow the <b>rabbits</b>."};
	// gui bools
	private bool tutMoveDone=false;
	private bool tutSitDone=false;
	private bool tutStandUpDone=false;
	private bool tutInteractDone=false;
	private bool tutFollowDone=true;// QUICK FIX to not show the last tutorial

	private const float DELAY_TIME=2.5f;
	private const float FADE_TIME=1.0f;
	private const float DARKOVERLAY_ALPHA=0.3f;
	private GameObject credits;
	private GUIText textOverlay;
	private GameObject darkOverlay;
	private GameObject currObj;
	private bool shown = true;
	private bool fade = false;
	private float currentTime=0.0f;
	private Tutorials nextTut = Tutorials.none;
	private Tutorials currTut = Tutorials.move;
	void Awake()
	{
		textOverlay = transform.FindChild("TextOverlay").gameObject.transform.FindChild("Text").guiText;
		darkOverlay = transform.FindChild("TextOverlay").gameObject.transform.FindChild("DarkOverlay").gameObject;
		resetTextOverlay(TutorialStrings[(int)currTut]);

		// start with movement		
		currTut = Tutorials.move;		
		
		credits = GameObject.Find("GUI").transform.FindChild("Credits").gameObject;
		credits.SetActive(false);
	}
	private void resetTextOverlay( string inStr )
	{
		textOverlay.text = inStr;
		GUIStyle style = new GUIStyle();
		style.font = textOverlay.font;
		
		Vector2 size = style.CalcSize(new GUIContent(textOverlay.text));
		Vector3 overlayScale = darkOverlay.transform.localScale;
		overlayScale.x  = size.x *0.00322f;
		darkOverlay.transform.localScale = overlayScale;
	}
	void Update()
	{
		if( fade )
		{
			currentTime += Time.deltaTime;
			if (shown) // fade out
			{
				fadeOutOverlay();
			}
			else if( !shown) // fade in
			{
				fadeInOverlay();
			}
		}
		if( !fade && !shown && (nextTut != Tutorials.none))
		{
			currentTime += Time.deltaTime;
			if( currentTime > DELAY_TIME)
			{
				//Prepare for the next tutorial
				chooseNextTutorial();
				resetTextOverlay( TutorialStrings[(int)nextTut] );
				fade = true;
				currentTime = 0.0f;
			}
		}
	}
	private void fadeOutOverlay()
	{
		float percent = (currentTime / FADE_TIME);
		if( percent > 1.0f)
		{
			percent = 1.0f;
			currentTime=0.0f;
			fade = false;
			shown = false;			
			currTut = Tutorials.none;
		}
		// DARKOVERLAY FADE OUT
		Color newDoColor = new Color(0.0f,0.0f,0.0f,DARKOVERLAY_ALPHA*(1.0f-percent));
		darkOverlay.renderer.material.color = newDoColor;
		// TEXT FADE OUT
		Color newTextColor = new Color(1.0f,1.0f,1.0f,(1.0f-percent));
		textOverlay.color = newTextColor;

	}
	private void fadeInOverlay()
	{
		if((nextTut == Tutorials.interact && tutInteractDone) // break off fade in if it changes during the fade in
			||(nextTut == Tutorials.sit && tutSitDone) 
			|| (nextTut == Tutorials.standup && tutStandUpDone))
		{
			shown = true;
			currentTime = 0.0f;
			return;
		}
		float percent = (currentTime / FADE_TIME);
		if( percent > 1.0f)
		{
			percent = 1.0f;
			currentTime=0.0f;
			fade = false;
			shown = true;
			currTut = nextTut;
			nextTut = Tutorials.none;
		}
		// DARKOVERLAY FADE OUT
		Color newDoColor = new Color(0.0f,0.0f,0.0f,DARKOVERLAY_ALPHA*(percent));
		darkOverlay.renderer.material.color = newDoColor;
		// TEXT FADE OUT
		Color newTextColor = new Color(1.0f,1.0f,1.0f,percent);
		textOverlay.color = newTextColor;
	}
	public void doneMove()		{ tutMoveDone=true; 	 if(currTut==Tutorials.move){ fade = true;nextTut=Tutorials.sit;}}
	public void doneSit()		{ tutSitDone = true; 	 if(currTut==Tutorials.sit) { fade = true;nextTut=Tutorials.standup;}}
	public void doneStandingUp(){ tutStandUpDone = true; if(currTut==Tutorials.standup) { fade = true;nextTut=Tutorials.interact;}}
	public void doneInteract()	{ tutInteractDone = true; if(currTut==Tutorials.interact) { fade = true;nextTut=Tutorials.follow;}}
	public void doneFollow()	{ tutFollowDone = true;
									GameObject.Find("GUI").transform.FindChild("Arrow").gameObject.SetActive(false); 
								  if(currTut==Tutorials.follow){ fade = true;nextTut=Tutorials.sit;}}

	private void chooseNextTutorial()
	{
		if(!tutInteractDone)
		{
			nextTut = Tutorials.interact;
			fade = true;
			return;
		}
		else if(!tutSitDone)
		{
			nextTut = Tutorials.sit;
			fade = true;
			return;
		}
		else if(!tutStandUpDone)
		{
			nextTut = Tutorials.standup;
			fade = true;
			return;
		}
		else if(!tutFollowDone)
		{
			nextTut = Tutorials.follow;
			fade = true;
			return;
		}
		else
		{
			nextTut = Tutorials.none;
			fade = true;
		}

	}
	public void showCredits()
	{		
		credits.SetActive(true);
		credits.guiTexture.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		StartCoroutine(DelayFadeIn(credits.guiTexture,1.0f));
	}
	
	IEnumerator DelayFadeIn(GUITexture fadein, float delay)
	{		
		yield return new WaitForSeconds(delay);
		if( fadein != null)
			StartCoroutine(Fade.use.Alpha(fadein, 0.0f, 1.0f, 3.0f));		
    }
	
}
