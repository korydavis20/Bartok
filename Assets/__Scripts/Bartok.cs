using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// This enum contains the different phases of a game turn
public enum TurnPhase {
	idle,
	pre,
	waiting,
	post,
	gameOver
}

public class Bartok : MonoBehaviour {
	
	static public Bartok S;
	static public Player CURRENT_PLAYER;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public Vector3 layoutCenter = Vector3.zero;
	public float handFanDegrees = 10f;
	public int numStartingCards = 7;
	public float drawTimeStagger = 0.1f;

	[Header("Set Dynamically")]
	public Deck deck;
	public List<CardBartok> drawPile;
	public List<CardBartok> discardPile;
	public List<Player> players;
	public CardBartok targetCard;
	public TurnPhase phase = TurnPhase.idle;

	private BartokLayout layout;
	private Transform layoutAnchor;

	void Awake() {
		S = this;
	}

	void Start () {
		deck = GetComponent<Deck>(); // Get the Deck
		deck.InitDeck(deckXML.text); // Pass DeckXML to it
		Deck.Shuffle(ref deck.cards); // This shuffles the deck

		layout = GetComponent<BartokLayout>(); // Get the Layout
		layout.ReadLayout(layoutXML.text); // Pass LayoutXML to it

		drawPile = UpgradeCardsList( deck.cards );
		LayoutGame ();
	}

	List<CardBartok> UpgradeCardsList(List<Card> lCD) { // a
		List<CardBartok> lCB = new List<CardBartok>();
		foreach( Card tCD in lCD ) {
			lCB.Add ( tCD as CardBartok );
		}
		return( lCB );
	}

	// Position all the cards in the drawPile properly
	public void ArrangeDrawPile() {
		CardBartok tCB;

		for (int i=0; i<drawPile.Count; i++) {
			tCB = drawPile[i];
			tCB.transform.SetParent( layoutAnchor );
			tCB.transform.localPosition = layout.drawPile.pos;
			// Rotation should start at 0
			tCB.faceUp = false;
			tCB.SetSortingLayerName(layout.drawPile.layerName);
			tCB.SetSortOrder(-i*4); // Order them front-to-back
			tCB.state = CBState.drawpile;
		}
	}

	// Perform the initial game layout
	void LayoutGame() {
		// Create an empty GameObject to serve as the tableau's anchor // c
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}


		// Position the drawPile cards
		ArrangeDrawPile();

		// Set up the players
		Player pl;
		players = new List<Player>();
		foreach (SlotDef tSD in layout.slotDefs) {
			pl = new Player();
			pl.handSlotDef = tSD;
			players.Add(pl);
			pl.playerNum = tSD.player;
		}

		players[0].type = PlayerType.human; // Make only the 0th player human

		CardBartok tCB;

		// Deal seven cards to each player
		for (int i = 0; i < numStartingCards; i++) {

			for (int j = 0; j < 4; j++) { // a
				tCB = Draw (); // Draw a card

				// Stagger the draw time a bit.
				tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j); // b

				players [(j + 1) % 4].AddCard (tCB); // c
			}
		}

		Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4+4));
	}

	public void DrawFirstTarget() {

		// Flip up the first target card from the drawPile
		CardBartok tCB = MoveToTarget( Draw () );

		// Set the CardBartok to call CBCallback on this Bartok when it is done
		tCB.reportFinishTo = this.gameObject;
	}

	// This callback is used by the last card to be dealt at the beginning
	public void CBCallback(CardBartok cb) { // c

		// You sometimes want to have reporting of method calls like this
		Utils.tr("Bartok:CBCallback()",cb.name); // d
		StartGame(); // Start the Game
	}

	public void StartGame() {
		// Pick the player to the left of the human to go first.
		PassTurn(1); // e
	}

	public void PassTurn(int num=-1) { // f
		// If no number was passed in, pick the next player
		if (num == -1) {
			int ndx = players.IndexOf(CURRENT_PLAYER);
			num = (ndx+1)%4;
		}
		int lastPlayerNum = -1;
		if (CURRENT_PLAYER != null) {
			lastPlayerNum = CURRENT_PLAYER.playerNum;
		}

		CURRENT_PLAYER = players[num];
		phase = TurnPhase.pre;

		CURRENT_PLAYER.TakeTurn();

		// Report the turn passing
		Utils.tr("Bartok:PassTurn()", "Old: "+lastPlayerNum, 
			"New: "+CURRENT_PLAYER.playerNum); 
	}

	// ValidPlay verifies that the card chosen can be played on the discard pile
	public bool ValidPlay(CardBartok cb) {

		// It's a valid play if the rank is the same
		if (cb.rank == targetCard.rank) return(true);
		// It's a valid play if the suit is the same
		if (cb.suit == targetCard.suit) {
			return(true);
		}
		// Otherwise, return false
		return(false);

	}


	// This makes a new card the target
	public CardBartok MoveToTarget(CardBartok tCB) {

		tCB.timeStart = 0;
		tCB.MoveTo(layout.discardPile.pos+Vector3.back);
		tCB.state = CBState.toTarget;
		tCB.faceUp = true;

		tCB.SetSortingLayerName("10");
		tCB.eventualSortLayer = layout.target.layerName;

		if (targetCard != null) {
			MoveToDiscard(targetCard);
		}

		targetCard = tCB;
		return(tCB);

	}

	public CardBartok MoveToDiscard(CardBartok tCB) {
		tCB.state = CBState.discard;
		discardPile.Add ( tCB );
		tCB.SetSortingLayerName(layout.discardPile.layerName);
		tCB.SetSortOrder( discardPile.Count*4 );
		tCB.transform.localPosition = layout.discardPile.pos + Vector3.back/2;
		return(tCB);
	}

	// The Draw function will pull a single card from the drawPile and return it
	public CardBartok Draw() {

		CardBartok cd = drawPile[0]; // Pull the 0th CardProspector
		drawPile.RemoveAt(0); // Then remove it from List<> drawPile
		return(cd); // And return it

	}


	/*
	 * // This Update() is temporarily used to test adding cards to players' hands
	void Update() { // d
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			players[0].AddCard(Draw ());
		}
		if (Input.GetKeyDown(KeyCode.Alpha2)) {
			players[1].AddCard(Draw ());
		}
		if (Input.GetKeyDown(KeyCode.Alpha3)) {
			players[2].AddCard(Draw ());
		}
		if (Input.GetKeyDown(KeyCode.Alpha4)) {
			players[3].AddCard(Draw ());
		}
	}
		*/
}