﻿using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts.InteractableBehaviour;
using UnityEngine;
using System.Linq;
using System.Collections;
using Assets.Scripts;

//public enum for use in ActiveTile and WorldGeneration as well
public enum Direction {North,South,East,West,None,NorthEast,NorthWest,SouthEast,SouthWest};
public enum LayerList {Default,b,c,d,e,f,g,h,ignorePebbleCollision,withPebbleCollision};
public enum CarryObject {Nothing, Flower, Clear, Berry, Branch}

public class PlayerController : MonoBehaviour {
	
	//publics
	public float speed;
	//public ActiveTile actTile;
	public Material playerMat;
	// public GroundGen groundGen;
	
	// gui elements
	public TutorialGui gui;
	public GameObject fire;
	private ProgressManager progressMng;
	private GUIText interactionTooltip;
	
	//privates
	private GameObject groundTile;
	public bool isSitting=false;
	private int movementMode = -1;
	protected Animator animator;
	// interactive stuff
	private float progress=0.0f;
	////private bool sit = false;
    ////private bool interact = false;
	private bool dead = false;
    private List<ActableBehaviour> inRangeElements;
    private const float THRESH_FOR_NO_COLLISION = 0.1f;
	private const float THRESH_FOR_PEBBLE_KICKING = 0.1f;
	private const float THRESH_FOR_TRUE_INTERACTION_TO_COUNT = 0.5f;
	private const float THRESH_FOR_SITTING_SOUND_FADEOUT = 5.0f; // in seconds
	private float currSittingTime = 0.0f; //100.0f for testing
	//Inertia
    private Direction lastDir = Direction.None;
	private float start = 0.0f;
	private float distance = 0.1f;
	private float duration = 1.0f;
	private float elapsedTime = 0.0f;
	private bool stopPlayerNow=false;
	private float lastH = 0.0f;
	private float lastV = 0.0f;
	public SphereCollider collisionHelper;
	private List<SphereCollider> collidingObj;
	//Sounds
	private AudioSource sittingSound;
	private AudioSource dyingSound;
	private float fadingSittingVolume;
    //Cam + Background
	private Camera isoCam;
	private Color background;
	// Debug
	private bool fadeOut=false;
	private float fadeOutFactor=0.2f;
    //Currently Pressing Sit & Interact
    private bool currentPressSit = false;
    private bool currentPressInteract = false;
	private bool currentPressFire = false;
    private bool currentPressG = false;
    //Carry
    public CarryElements Carry;

	public int branchInventory = 0;
    

    //get the collider component once, because the GetComponent-call is expansive
	void Awake()
	{
		//groundGen = this.GetComponent<GroundGen>;
		inRangeElements = new List<ActableBehaviour>();
		animator = transform.Find("animations").GetComponent<Animator>();
		groundTile = GameObject.Find("GroundTile");
		progressMng = (ProgressManager)GameObject.Find("Progression").GetComponent("ProgressManager");
		interactionTooltip = GameObject.Find("ContextSensitiveInteractionText").GetComponent<GUIText>();
		interactionTooltip.text = "";
	    //Carry = gameObject.GetComponentInChildren<CarryElements>();

		
		// find sounds
		sittingSound = GameObject.Find("AudioSit").GetComponent<AudioSource>();
		dyingSound = GameObject.Find("AudioDeath").GetComponent<AudioSource>();
		// find camera
		isoCam = GameObject.Find("IsoCamera").GetComponent<Camera>();
		background = isoCam.backgroundColor;
		// set to no collisions with pebbles (via Layers)		
		SetLayerRecursively(gameObject,(int)(LayerList.ignorePebbleCollision)); // ignorePebbleCollision
		// colliding stuffs
        collisionHelper = transform.Find("ObstacleCollider").gameObject.GetComponent<SphereCollider>();
		collidingObj = new List<SphereCollider>();

		setPlayersYPosition();
	}

	void Update () {
		if (dead) {
			return;
		}
		// Cache the inputs.
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
		if( h != lastH || v != lastV)
			stopPlayerNow = false;
		//Debug.Log (h.ToString()+ " "+ v.ToString()) ;
		// changed the Axis gravity&sensitivity to 1000, for more direct input.
		// for joystick usage however Vince told me to:
		/* just duplicate Horizontal and use gravity 1, dead 0.2 and sensitivity 1 that it works*/		
        ////sit = Input.GetButtonDown("Sit");
		////interact = Input.GetButtonDown("Interact");
		
		checkProgress();

        Action();
				
        if( Input.GetButtonDown("ToggleMovementMode"))
		{	movementMode++;
			if( movementMode > 2)
				movementMode = 0;
		}
		
		if ( !isSitting)
		{	
			if(!stopPlayerNow)
				Movement(v,h);
		}	
		else
		{
			currSittingTime += Time.deltaTime; // count seconds spend sitting;
		}
		FadeSounds(Time.deltaTime);
		// if (progress > 0.5f) {		
			DisplayInteractionTooltip();		
		// }
		animationHandling();
	    Carry.UpdateCarry(progress);
		lastH = h;
		lastV = v;

		if (branchInventory > 0) {

		}

				//for testing only
		if (Input.GetKeyDown ("space")) {

			Debug.Log(inRangeElements.First()); 

		}

	}

    void Action()
    {
        bool pressInteract = Input.GetButton("Interact");
        if (pressInteract & !currentPressInteract)
        {
            Interact();
            currentPressInteract = true;
        }
        else if (!pressInteract & currentPressInteract)
        {
            currentPressInteract = false;
        }

        bool pressSit = Input.GetButton("Sit");
        if (pressSit & !currentPressSit)
        {
            Sit();
            currentPressSit = true;
        }
        else if (!pressSit & currentPressSit)
        {
            currentPressSit = false;
        }

        bool pressG = Input.GetKey(KeyCode.G);
        if (pressG & !currentPressG)
        {

            Carry.ThrowBouquet();
            currentPressG = true;
        }
        else if (!pressG & currentPressG)
        {
            currentPressG = false;
        }

		bool pressFire = Input.GetButton("Fire");
        if (pressFire & !currentPressFire)
        {
            MakeFire();
            currentPressFire = true;
        }
        else if (!pressFire & currentPressFire)
        {
            currentPressFire = false;
        }
    }

    void Sit()
    {
        if (!isSitting)
        {
            // hide how to sit in the gui
            gui.doneSit();
            //sit down
            currSittingTime = 0.0f;
            animator.SetBool("sitting", true);
            //change the carrying position when sitting down
            transform.Find("CarryingPosition").gameObject.transform.Translate(0.0f, -0.15f, 0.0f);
            isSitting = true;
            PlaySittingSound();
            elapsedTime = duration;
        }
        else
        {
            gui.doneStandingUp();
            //stand up again

            animator.SetBool("sitting", false);
            //change the carrying position when standing up again
            transform.Find("CarryingPosition").gameObject.transform.Translate(0.0f, +0.15f, 0.0f);
            StopSittingSound(currSittingTime);
            isSitting = false;
            //progressMng.usedMechanic(ProgressManager.Mechanic.Sitting, currSittingTime);
            currSittingTime = 0.0f;
        }
    }

    void Interact()
    {
        InteractableBehaviour closest = FindClosestInteractable();
        if (closest != null)
        {
            CarryObject co = closest.Activate(progress, ToPlayerPos(closest));
            if (co == CarryObject.Flower)
            {
                ClearInventory();
				animator.SetBool("picking", true);
                Carry.PickFlower(progress);
            }
            if (co == CarryObject.Berry)
            {
                animator.SetBool("eating", true);
                Carry.EatBerry(progress);
            }
			if (co == CarryObject.Branch) {
				ClearInventory();
				animator.SetBool("picking", true);
				Carry.PickBranch(progress);
			}

			
            gui.doneInteract();
            if (progress >= THRESH_FOR_TRUE_INTERACTION_TO_COUNT)
                progressMng.usedMechanic(ProgressManager.Mechanic.Interaction);
			// stop players movement
			stopPlayerNow = true;
			lastDir = Direction.None;
			elapsedTime = duration;
        }
    }

	private void MakeFire() {

		if (branchInventory >= 3) {
			Vector3 position = transform.position;
			position = position+(transform.right*1.1f);
			position.y = groundTile.GetComponent<GroundGen>().returnPlayerPos(position.x, position.z) - 0.2f;
			Instantiate(fire, position, Quaternion.identity);
			branchInventory -= 3;
			if (branchInventory < 1) {
				ClearInventory();
			}
		}
	}

	private void animationHandling()
	{
		AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
		if(stateInfo.fullPathHash == Animator.StringToHash("Base.Picking"))
		{
			animator.SetBool("picking", false );
		}
		else if(stateInfo.fullPathHash == Animator.StringToHash("Base.Kicking"))
		{
			animator.SetBool("kicking", false );
		}
		else if(stateInfo.fullPathHash == Animator.StringToHash("Base.Dying"))
		{
			animator.SetBool("dead", false);
		}
        else if (stateInfo.fullPathHash == Animator.StringToHash("Base.Eating"))
        {
            animator.SetBool("eating", false);
        }
	}

    private Vector3 ToPlayerPos(ActableBehaviour actable)
    {
        Vector3 toPlayerVec = (transform.position - actable.transform.position);
        toPlayerVec.y *= 0;
        return toPlayerVec;
    }

	public static void SetLayerRecursively(GameObject go, int layerNumber)
	{
		foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
		{
			trans.gameObject.layer = layerNumber;
		}
	}

    private void checkProgress()
    {
		if(isSitting)
			progressMng.usedMechanic(ProgressManager.Mechanic.Sitting, Time.deltaTime);
		// compute new progress value:
//		progressMng.computeProgress();
		progress = progressMng.getProgress();
		// set attributes accordingly

		//set player char opacity
		//float grey = progressMng.getValue(ProgressManager.Values.GreyPlayerColor);
		float grey = 0.6f;
		playerMat.color = new Color(grey,grey,grey,  progressMng.getValue(ProgressManager.Values.Alpha)); // transparency

		// rigidbody.isKinematic = progress <= THRESH_FOR_NO_COLLISION; // starts colliding

		speed =  progressMng.getValue(ProgressManager.Values.Speed); // reduced speed
		duration = progressMng.getValue(ProgressManager.Values.InertiaDuration); // reduced sliding
		distance = progressMng.getValue(ProgressManager.Values.InertiaDistance); // reduced sliding
		
		if (progress > THRESH_FOR_PEBBLE_KICKING)
		{			
			SetLayerRecursively(gameObject,(int)(LayerList.withPebbleCollision));
		}
		
		//start fading the background to black				
		// float lower = progressMng.getValue(ProgressManager.Values.BackgroundColorFactor);
		// isoCam.backgroundColor = new Color ( background.r*lower , background.g*lower, background.b*lower, 1.0f);
	
		// he dies at progress 1.0f
		// if (progress > 1.0f && !dead)
		// {
		// 	Die();
		// }
    }

    private void Movement(float v, float h)
	{
		Direction moved = Direction.None;
		if( movementMode == 0 || movementMode ==-1) // DPAD mode
		{			
			if( v > 0.05f )
				moved = Direction.North;
			else if( v < -0.05f )
				moved = Direction.South;
			
			if( h < -0.05f )
			{
				if(moved == Direction.North)
					moved = Direction.NorthWest;
				else if ( moved == Direction.South)
					moved = Direction.SouthWest;
				else
					moved = Direction.West;
			}
			else if( h > 0.05f )
			{			
				if(moved == Direction.North)
					moved = Direction.NorthEast;
				else if ( moved == Direction.South)
					moved = Direction.SouthEast;
				else
					moved = Direction.East;
			}
		}
		else if( movementMode == 1) // diagonal mode version
		{			
			if( v > 0.05f )
				moved = Direction.NorthWest;
			else if( v < -0.05f )
				moved = Direction.SouthEast;
			if( h < -0.05f )	
			{
				if(moved == Direction.NorthWest)
					moved = Direction.West;
				else if ( moved == Direction.SouthEast)
					moved = Direction.South;
				else
					moved = Direction.SouthWest;
			}
				
			else if( h > 0.05f )
			{
				if(moved == Direction.NorthWest)
					moved = Direction.North;
				else if ( moved == Direction.SouthEast)
					moved = Direction.East;
				else
					moved = Direction.NorthEast;
			}
		}
		else if( movementMode == 2) // diagonal mode 
		{			
			movementMode=-1;			
		}
		// apply the movement-vector to the player if he moved
		if(moved != Direction.None)			
		{		
			gui.doneMove();
			switch(moved)//rotation
			{
				case Direction.North: gameObject.transform.eulerAngles = new Vector3(0.0f,0.0f,0.0f);
					break;
				case Direction.East: gameObject.transform.eulerAngles = new Vector3(0.0f,90.0f,0.0f);
					break;
				case Direction.South: gameObject.transform.eulerAngles = new Vector3(0.0f,180.0f,0.0f);
					break;
				case Direction.West: gameObject.transform.eulerAngles = new Vector3(0.0f,270.0f,0.0f);
					break;
				case Direction.NorthEast: gameObject.transform.eulerAngles = new Vector3(0.0f,45.0f,0.0f);
					break;
				case Direction.NorthWest: gameObject.transform.eulerAngles = new Vector3(0.0f,315.0f,0.0f);
					break;
				case Direction.SouthEast: gameObject.transform.eulerAngles = new Vector3(0.0f,135.0f,0.0f);
					break;
				case Direction.SouthWest: gameObject.transform.eulerAngles = new Vector3(0.0f,225.0f,0.0f);
					break;
			}		
			lastDir = moved;			
			Vector3 dir = CheckForCollisions(moved);			
			gameObject.transform.Translate(dir*Time.deltaTime*speed*0.1f); //move forward a step		
			elapsedTime = 0.0f;
		}
		else if(elapsedTime <= duration && progress < THRESH_FOR_NO_COLLISION) // inertia
		{
			Interpolate.Function test = Interpolate.Ease(Interpolate.EaseType.EaseOutSine);
			float incVal = test(start, distance,elapsedTime, duration);
			//Debug.Log ("val:"+incVal+" s:"+start + " d:"+distance+" elT:"+elapsedTime+" dur:"+duration);			
			gameObject.transform.Translate(new Vector3( distance-incVal,0.0f,0.0f)*Time.deltaTime*speed);
			elapsedTime += Time.deltaTime;			
			moved = lastDir;
		}
		
		collidingObj.Clear();
		//set the players Y pos depending on the terrain
		// only if he was moved by player or inertia
		if( moved != Direction.None) 
		{
			setPlayersYPosition();
		}
	}
	
    private Vector3 CheckForCollisions(Direction moved)
	{
		Vector3 ret = new Vector3(1.0f,0.0f,0.0f);
		float collSizePercent = progressMng.getValue(ProgressManager.Values.CollisionSizePercent);
		if (collSizePercent == 0.0f) // do not compute collisions when the CollisionSizePercent is zero
			return ret;
		foreach( SphereCollider enemy in collidingObj)
		{
			//compute collision-vector between this-SphereCollider and the other-SphereCollider
			Vector3 dif = collisionHelper.transform.position - enemy.transform.position;
			// ignore Y-difference
			dif.y = 0.0f;
			if(((collisionHelper.radius + enemy.radius)*collSizePercent) > dif.magnitude)
			{
				// convert the collision-vector to local space, because player rotates in the movementfunction
				dif = transform.InverseTransformDirection(dif);	
	    		// offset the player frame & speed independent 
				// *0.7f makes sure that diagonal movement should be save ( 1 / sqrt(2) )
				ret.Set((dif.x)/(Time.deltaTime*speed*1.0f), ret.y,(dif.z)/(Time.deltaTime*speed*1.0f));
			}
		}			
		return ret;
	}	
    
    private void setPlayersYPosition()
	{
		float newYPos = gameObject.transform.position.y;
		try
		{
			newYPos = groundTile.GetComponent<GroundGen>().returnPlayerPos(gameObject.transform.position.x,gameObject.transform.position.z);
		}
		catch(System.MissingMethodException e)
		{
			newYPos = gameObject.transform.position.y;
		}
		float diff = newYPos-gameObject.transform.position.y;
		if (Mathf.Abs(diff) > 0.0001f)
			gameObject.transform.Translate(new Vector3(0.0f,newYPos-gameObject.transform.position.y+0.585f,0.0f));
	
	}	
	
    public void channeledTriggerStay (Collider other)
	{
		if( other.name == "CollisionCollider")
		{			
			SphereCollider enemy = other.GetComponent<SphereCollider>();
			if (enemy != null)
			{
				collidingObj.Add( enemy );				
			}
		}
	}
    
    public void channeledTriggerEnter (Collider other)
	{
		if( other.gameObject.tag == "NextTileTriggers")
		{
			//teleport to new position
			Direction dir=Direction.None;
			if( other.name == "WestTrigger")
			{
				dir = Direction.West;
				
				//gameObject.transform.Translate(new Vector3((-other.gameObject.transform.position.x*2)-2.0f,0.0f,0.0f),Space.World); // move to east MIKE old code
				
				Vector3 position = gameObject.transform.position;
				position.z = 1;
				gameObject.transform.position = position;
                inRangeElements.Clear();
				
			}
			else if( other.name == "EastTrigger")
			{
				dir = Direction.East;
				
				//gameObject.transform.Translate(new Vector3(-(other.gameObject.transform.position.x*2)+2.0f,0.0f,0.0f),Space.World); // move to west 
				
				Vector3 position = gameObject.transform.position;
				position.z = 18;
				gameObject.transform.position = position;
                inRangeElements.Clear();
				
			}
			else if( other.name == "NorthTrigger")
			{
				dir = Direction.North;
				
				//gameObject.transform.Translate(new Vector3(0.0f, 0.0f, -(other.gameObject.transform.position.z*2)+2.0f),Space.World); // move to south 
				
				Vector3 position = gameObject.transform.position;
				position.x = 1;
				gameObject.transform.position = position;
                inRangeElements.Clear();
			}
			else if( other.name == "SouthTrigger")
			{
				dir = Direction.South;
				//gameObject.transform.Translate(new Vector3(0.0f, 0.0f, (-other.gameObject.transform.position.z*2)-2.0f),Space.World); // move to south 
				
				Vector3 position = gameObject.transform.position;
				position.x = 18;
				gameObject.transform.position = position;
                inRangeElements.Clear();
			}
			
			// update tile, pass the direction along
			groundTile.GetComponent<GroundGen>().showNextTile(dir);
			gui.doneFollow();
			if( gui.firstTileDone() )
				gui.doneSecondTile();
			gui.doneFirstTile();


			progressMng.usedMechanic(ProgressManager.Mechanic.Travel);

			setPlayersYPosition();
		}
		if( other.gameObject.tag == "Interactable")
		{
			Transform colli = other.transform.Find("CollisionCollider");
			if( colli != null)
			{
				SphereCollider enemy = colli.GetComponent<SphereCollider>();
				if (enemy != null)
					collidingObj.Add( colli.GetComponent<SphereCollider>() );				
			}
			
			ActableBehaviour addThis = other.GetComponent<ActableBehaviour>();

			inRangeElements.Add(addThis);
			
			if( progress < THRESH_FOR_TRUE_INTERACTION_TO_COUNT)
			{
				progressMng.usedMechanic( ProgressManager.Mechanic.Interaction );
			}
		}
		else if( other.name == "CollisionCollider")
		{			
			SphereCollider enemy = other.GetComponent<SphereCollider>();
			if (enemy != null)
			{
				collidingObj.Add( enemy );
			}
		}
	}
	
    private InteractableBehaviour FindClosestInteractable()
	{
	    List<InteractableBehaviour> interactable = inRangeElements.Where(e => e is InteractableBehaviour).Cast<InteractableBehaviour>().ToList();
		if( interactable.Count < 1)
			return null;
        //Slightly clunky
	    return interactable.First(
	            e =>
	            Vector3.Distance(transform.position, e.transform.position) <=
	            interactable.Min(f => Vector3.Distance(transform.position, f.transform.position)));
	}

	private void DisplayInteractionTooltip()
	{
		if ( dead )
			return;
		
		interactionTooltip.text = "";
		InteractableBehaviour closest = FindClosestInteractable();
//		Debug.Log(closest.name);
		if( closest != null) 
        {
			if (closest.customInteractiveText() != null)
				if (closest.name == "Flower(Clone)") {
					interactionTooltip.text = "Press <b>E</b> "+closest.customInteractiveText();
				} else if (progress >= 0.5f) {
					interactionTooltip.text = "Press <b>E</b> "+closest.customInteractiveText();
				}
		} else if (branchInventory >= 3) {
			// Debug.Log("here");
			interactionTooltip.text = "Press <b>F</b> to make fire";
		}			
	}
	
    public void channeledTriggerExit(Collider other)
    {
        switch (other.gameObject.tag)
        {
            case "Interactable": 
				ActableBehaviour removeThis = other.GetComponent<ActableBehaviour>();
				if (removeThis.GetType() == typeof(ReactableBehaviour))
				{
					((ReactableBehaviour)removeThis).Deactivate();
				}
	//					Debug.Log("Removing" + removeThis.name);
				inRangeElements.Remove(removeThis);
			break;
        }
    }

    void OnGUI()
    {
		const int x = 25;
		const int y = 300;
        if (movementMode != -1)
        {
            //speed slider
            GUI.Label(new Rect(x, y + 40, 120, 20), "Speed: " + speed.ToString(CultureInfo.InvariantCulture));
            speed = GUI.HorizontalSlider(new Rect(x, y + 60, 300, 10), speed, 0f, 500.0f);

            //Inertia Duration slider
            GUI.Label(new Rect(x, y + 80, 200, 20), "InertiaDuration: " + duration.ToString("#.###"));
            duration = GUI.HorizontalSlider(new Rect(x, y + 100, 300, 10), duration, 0.0f, 2.0f);
			
			//Inertia Distance slider
			GUI.Label(new Rect(x, y + 120, 200, 20), "InertiaDistance: " + distance.ToString("#.###"));
            distance = GUI.HorizontalSlider(new Rect(x, y + 140, 300, 10), distance, 0.0f, 1.0f);

            //movement style slider
            GUI.Label(new Rect(x, y + 160, 150, 20), "AlternateMoveStyle:");
            float test = GUI.HorizontalSlider(new Rect(x, y + 180, 50, 10), movementMode, 0.0f, 2.0f);

            //in range elements count
            GUI.Label(new Rect(x, y + 220, 100, 20), "Debug:");
            //GUI.Label(new Rect(x, y + 240, 200, 20), inRangeElements.Count.ToString(CultureInfo.InvariantCulture));
            //GUI.Label(new Rect(x, y + 200, 200, 20), inRangeElements.OfType<RabbitGroupBehavior>().Count().ToString(CultureInfo.InvariantCulture));
            GUI.Label(new Rect(x, y + 240, 100, 20), Carry.carryList.Count.ToString(CultureInfo.InvariantCulture));

            if (test > 1.5f)
                movementMode = 2;
            else if (test > 0.5f)
                movementMode = 1;
            else
                movementMode = 0;
        }
    }
	private void Die()
	{
		dead = true;
		// change mesh to lying 
		animator.SetBool("dead", true );
		// start Death sounds
		PlayDeathSound();
		// clean the interaction Tooltip text
		interactionTooltip.text = "You died.";	
		// show the credits
		gui.showCredits();
	}
	private void FadeSounds(float timeDelta)
	{
		if(sittingSound.GetComponent<AudioSource>().isPlaying)
		{
			if (!fadeOut) // fade In
			{
				if ( fadingSittingVolume < 1.0f)
					fadingSittingVolume += timeDelta*0.2f;
				sittingSound.GetComponent<AudioSource>().volume = fadingSittingVolume;			
			}
			else // fade Out
			{
				if ( fadingSittingVolume > 0.01f)
					fadingSittingVolume -= timeDelta*fadeOutFactor;
				else 
				{
					sittingSound.GetComponent<AudioSource>().Stop();
					fadeOut = false;
				}
				sittingSound.GetComponent<AudioSource>().volume = fadingSittingVolume;							
			}
		}
	}
	private void PlaySittingSound()
	{
		sittingSound.GetComponent<AudioSource>().time = sittingSound.GetComponent<AudioSource>().clip.length*Random.value; // starts at random pos in the track
		sittingSound.GetComponent<AudioSource>().Play();
		fadingSittingVolume = 0.0f;
	}
	private void StopSittingSound(float inTimeSpentSitting)
	{		
		if ( inTimeSpentSitting < THRESH_FOR_SITTING_SOUND_FADEOUT)
			fadeOutFactor = 100.0f;
		else
			fadeOutFactor = Mathf.Lerp(0.5f, 0.03f, Mathf.Min (1.0f, (inTimeSpentSitting-THRESH_FOR_SITTING_SOUND_FADEOUT)/20.0f));
		fadingSittingVolume = 1.0f;
		sittingSound.GetComponent<AudioSource>().volume = fadingSittingVolume;
		fadeOut = true;
	}
	private void PlayDeathSound()
	{
		StopSittingSound(0.0f);
		dyingSound.Play();
	}

	public void AddToInventory(string loot) {

		switch(loot) {
			case "branch":
				branchInventory++;
				Debug.Log(branchInventory);
			break;
			case "flower":
			break;
			case "mushroom":
			break;
		}

	}

	public void ClearInventory() {
		Transform[] children = GameObject.Find("CarryingPosition").GetComponentsInChildren<Transform>();

		foreach (Transform child in children) {
			if (child.name != "CarryingPosition") {
				child.gameObject.SetActive(false);
			}
		}
	}
}