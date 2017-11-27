using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bartok : MonoBehaviour {
	
	static public Bartok S;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public Vector3 layoutCenter = Vector3.zero;
	public float handFanDegrees = 10f;

	[Header("Set Dynamically")]
	public Deck deck;
	public List<CardBartok> drawPile;
	public List<CardBartok> discardPile;
	public List<Player> players;
	public CardBartok targetCard;

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
	}

	// The Draw function will pull a single card from the drawPile and return it
	public CardBartok Draw() {
		CardBartok cd = drawPile[0]; // Pull the 0th CardProspector
		drawPile.RemoveAt(0); // Then remove it from List<> drawPile
		return(cd); // And return it
	}

	// This Update() is temporarily used to test adding cards to players' hands
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

}