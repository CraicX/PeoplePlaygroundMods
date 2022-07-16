//                             ___           ___         ___     
//  Feel free to use and      /\  \         /\  \       /\__\    
//  modify any of this code   \:\  \       /::\  \     /:/  /    
//                             \:\  \     /:/\:\__\   /:/  /     
//   ________  ________    _____\:\  \   /:/ /:/  /  /:/  /  ___ 
//  |\   __  \|\   __  \  /::::::::\__\ /:/_/:/  /  /:/__/  /\__\
//  \ \  \|\  \ \  \|\  \ \:\~~\~~\/__/ \:\/:/  /   \:\  \ /:/  /
//   \ \   ____\ \   ____\ \:\  \        \::/__/     \:\  /:/  / 
//    \ \  \___|\ \  \___|  \:\  \        \:\  \      \:\/:/  /  
//     \ \__\    \ \__\      \:\__\        \:\__\      \::/  /   
//      \|__|     \|__|       \/__/         \/__/       \/__/     
//
//
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;


namespace PPnpc
{
	class NpcChat : MonoBehaviour
	{
		public TextMeshPro ChatMessage;
		public GameObject ChatBubble;
		public GameObject ChatBG;
		public NpcBehaviour NPC;
		public RectTransform rect;
		public SpriteRenderer SR;
		
		public bool IsActive      = false;
		public Vector3 Offset     = new Vector2(1f,1f);
		private bool _lastFlipped = true;
		public string Message     = "";
		public float Timer        = 1f;

		public static Dictionary<string, List<string>> SmackList = new Dictionary<string, List<string>>();
		
		public static Dictionary<string, string[]> SmackText = new Dictionary<string, string[]>()
		{
			{"misc", new string[]{ 
				"Your face makes me want to punch babies", "You're on the wrong side of town", "I'll punch you in your goofy face", 
				"I ought to stoot-slap your *** right now", "You wanna fat lip?", "Live or die, man?", " **** you!", 
				"I think I'll rearrange your face", "You had enough yet, dumbass?", "Come at me bro!", "I'll kick your ***", 
				"You have a pretty mouth", "You have a problem?!", "What you looking at?", "I'll punch you in the ****", 
				"How'd you like a knuckle sandwich?", "You wanna piece of me?", "I'm taking you down", "Settle in for your whompin", 
				"You wanna scrap?", "Let's do this", "You don't know me, fool", "I'm going to enjoy this", 
				"Don't even look at me, chump", "You wanna fight?", "I'm not scared of you", "Back up!", 
				"Yo, back the hell off me", "Your *** is grass", "I ain't lost a fight yet", "Mama said knock you out!", 
				"You're mine!", "I'm bringing the pain!", "You better not get blood on my shirt", 
				"I eat pieces of **** like you for breakfast!", "Time for your beatdown", "Why dont you sit down and shut up", 
				"Don't make me slap the **** out of you!", "Your breath smells like gorilla ***",  
				"If I had a rubber hose I'd beat you with it", "Next time I'll break my foot off in yo ***", 
				"You're going to lose this fight", "You're weak, punk ***!", "What are you looking at, butthead?", } },

			{"bat", new string[]{"Batter up, *****!", "Batter up!", "Come get some", "You better run!", "Play ball!", } },

			{"knife", new string[]{
				"Who said anything about slicing you up?", "I'm just gonna slice a little 'Z' in your forehead", "I'll cut you sucka!", 
				"I'll cut your head off!", "I'm feeling really stabby", "I'll shiv you fool!", "I'll gut you like a fish!", 
				"Ears and noses will be the trophies of the day", "You see this knife?", "I'll cut out your tongue", 
				"I'll festoon my bedchamber with your guts", "Yo homey! Blood in, blood out!", } },

			{"call", new string[]{
				"Hey!", "Yo, dirtbag!", "Excuse me!", "Dude", "Come here!", "Hey yo!", "Yo!", "Wait up", "Stop!", "Get over here!",
				"Halt!", "Where you going?", "Where you think you're going?", "That's right, run away!", "You coward!", "Chicken!",
				"Yellow belly", "Hey douchebag!", "Quit walking away and fight me!", "Thats right, run home to your mama", 
				"Chicken s***!", "Go wash your mangyna", "I'ma git you sucka!", "Ya bish!", "Turn around, now!",
				"Face me!", "I challenge you to a fight!", "Hey you want a piece of me?", "You can run but you cant hide!", 
				"I'll find where you live", "You can't run forever!", "You'll have to fight me eventually!", "You suck!",
			} },

			{"sword", new string[]{ 
				"Which limb you want me to chop off?", "Things are about to get messy", "The sword is mightier", 
				"en garde senior pussy cat", "What now fool?", "I wanna show you this sword", "Now... Lets chop that head off", 
				"There can be only one!", "Today, I eat man flesh!", "I cannot sheathe my sword until it's tasted blood!", } },

			{"handgun", new string[]{
				"Where's my money, *****?!", "Dodge this!", "Taste lead, dirtbag!", "You feel lucky, punk?", 
				"Ima pop a cap, fool!", "My trigger finger is getting itchy, keep talkn", "You in my hood now, mother flower!", 
				"I'll bust a cap in yo ***", "Time to die", "You're the disease, I'm the cure", "S#!% just got real!", 
				"Get off my plane!", "Your move, creep", } },

			{"rifle", new string[]{ 
				"I'm a gangsta", "Let's tear some **** up!", "Rat-a-tat-tat-dat-***", "Thats right, I'm strappin", 
				"Who wants some?!",  "It's time!", "Knock knock!", "Eat this!", "Here comes the pain!", 
				"How does it feel to be hunted?", "Welcome to Hell, *****!", "I'll send you home in a body bag", } },

			{"death", new string[] { 
				"Suck it!", "How you like them apples?", "In Nomine Patris, et Fili, et Spritus Sancti", "Oops, I did it again", 
				"Whoops, he dead", "I have slayed thee", "Justice has been served!", "I am invincible!", 
				"Good, I'm glad he's dead", "That'll do, pig. That'll do.", "Nobody puts Baby in the corner", 
				"Happy trails, buttface", "Adios, mother flower!", "Give my regards to King Tut, ***hole!", 
				"You better not find a rebirther neither!", "Another one bites the dust!", "You dead, sucka!", 
				"I'm the best!", "You lose!", "No one's going to your funeral",  "Rest in pieces", 
				"It's just been revoked!", "Hasta la vista, baby", "Game over!", "See you in hell", "This is SPARTA!", 
				"You have been erased!", "King Kong aint got **** on me!", "Too easy!", "I am a champion!", 
				"You're worm food now", } },

			{"response", new string[] { "I'm busy, dont bother me", "I dont want to fight you", "I only fight at the dojo", 
				"I dont raise my hands in anger!", "It take all day to think of that?", "Good one, idiot!", "Ooh I'm really scared", 
				"Kick rocks, jerko", "Beat it, freak!", "Look pal, I dont want trouble", "Say hi to your sister for me", 
				"Yeah yeah yeah... shut up", "Kiss my grits!", "This is your last warning!", "Shut your tiny mouth now!", 
				"You're cruisin for a bruisin", "B****, yo mama!", "Don't push me, mother flower!", 
				"Let's not devolve into animals", "You wanna go?!", "Oh! I get it... you wanna get knocked the **** out!", 
				"You can't talk that way to me", "What the hell is your problem?", "What? I don't even know who you are!", 
				"Crap off!", "You're no longer my best friend", "You shall not pass!", "Why dont you say that to my face?", 
				"You talk'n to me, tough guy?", "Whatever", "Sir, you are being very rude!", "You kiss your mother with that mouth?", 
				"You can't hurt my feelings", "Shut that filthy mouth!", "How dare you!", 
				"You dont know who you're messing with, punkass", "Oh, you want to throw down?!", "You owe me an apology", 
				"How bout I just beat the dog crap outta you?", "What belt are you, bro?", "What?! You wanna rumble?", 
				"This aint some video game, dude, this is real life!", "We dont have to do this, man!", "You drew first blood, not me!", 
				"nah bruh, I won't even break a sweat", "You talk too much", "Give it a rest", "Enough jibber jabber, lets fight!", 
				"Yo mama", "I know karate", "Just because I rock, doesn't mean I'm made of stone", "I dont talk to strangers",
				"Uhm you're a stranger danger creep!", "No thanks, I dont want anything", "My sensai says I can't fight chicks",
				"Sure pal, keep talking", "Alrighty then", "Blah blah blah, shut the **** up","You're about to see my fists of fury", 
				"I'll karate chop your **** off", "You done talking?", "Talk is cheap, ***hole", "I know you are but what am I?",
				"Takes one to know one", "You must be talk'n about yo mama",} },

			{"mercy", new string[] { 
				"Cry baby!", "Pfft! You too pathetic to smoke!", "Fine! You better not tell anyone!", "Everyone is scared of me",
				"Next time I wont be so nice", "Today's your lucky day", "You ain't even worth it", "Hahaha, I'm one bad mamma jamma!", 
				"Don't poop your pants.", "Alright then, you're cool I guess", "You are forgiven", "I forgive you for being a *****", 
				"I'll let you live but next time give me $20", "Yeah thats right!", "Go cry to your mama!", "OK, chicken ****!", 
				} },

			{"move", new string[] {
				"Can you move?", "Excuse me, please", "Move your *** out my face", "Hey yo! Personal space!", 
				"Move it or lose it", "I'll count to 3 then you better be somewhere else", "You smell like brocolli", 
				"I was here first", "How rude!", "Can I help you?!", "Are you trying to piss me off?", "Did you take my wallet?", 
				"I dont like you", "Get out the way, dumbass", "Did you just grope me?", "Don't goose me you pervert!", "Watch it!",
				"Move! Get out the way!", "I'm not your friend", "Just beat it", "Move along", "You lost or something?", 
				"You looking for trouble?", "You kind of suck", "Wow! At least say excuse me!", "Oh, no you didn't!", 
				"I may have to teach you some manners", "I'll dunk your head like a basketball", "You ever been punched in the nose?",
				"You and me got business!", "Lets settle this!", "Buttface", "Another idiot in my way", "I'm taking over this town",
				"You aint nothing but a chump", "You want to see my Karate?", "You dumb!",} },

			{"hurt", new string[] {
				"I think I need medical attention.", "Hey buddy, you have a medkit?", "Help me up so I can kick your ***!", 
				"I dont feel so good", "Can't say I feel 100%", "Yeah you would kick a man when he's down", "Take it easy", 
				"I'll get my revenge", "You are so annoying", "Dude.. get lost", } },

			{"down", new string[] { 
				"I feel sorry for you", "Come on get up!", "You on the ground like a bum!", "It's so sad to see wimps like this",
				"Thats right! Bow before Zod!", "You lazy piece of ****!", "Get up, coward!", "Move along buddy! This aint your house.", 
				"Look at this chump", "What's the matter with your legs?" , "I bet you wish you had legs like mine", "Get the **** up!", 
				"You're blocking peoples path, move!", "I don't have any spare change for you, bum!", "Do you need some help getting up?", 
				"Thats right sucka!", "You limp wimp!", "Bow down to yo masta!", "Get up and step up", "Step up to get beat down", 
				"Pfft! You're faking injuries", "How long you going to sit on your ***?"} },

			{"warn", new string[]
			{
				"Freeze dirtbag!", "Don't move!", "Don't come any closer!", "That's far enough!", "Stay away!", "I have a gun, dumbass!",
				"Stop!", "Last warning!", "Get on your knees!", "You wanna die?", "Dafuq?", "You see this gun?", "I'll smoke you, fool!",
				"Get back!", "I'll kill you", "I'm warning you, b****!",
			} },

			{"witness", new string[] { 
				"Karate for self defense only!", "Red & Blue, Cousin, Blood, It just dont matter!", "Inside you have strong root.", 
				"Oooh sweet! I hate that guy", "Light him on fire!", "Kill his dumb face!", "Beat the pudding out that fool!", 
				"Why you guys fighting?", "Hey stop that now!", "Don't do that!", "I'll take you both on!", 
				"Good good, feel the hate!", "Show him who's boss!", "Bash his face in!", "That's messed up, man", 
				"Why dont you just leave him alone", "He's had enough man!", "Why dont you pick on someone your own size!", 
				"Yeah! Kick his ***!", "Bruh, I bet he kicks your butt", "This used to be a friendly town", 
				"Whats wrong with people these days?", "Boy, this place is rough", "Why you guys always fighting?", 
				"Can't we all just get along?", "Nice! Roll his ***!", "I ain't seen an *** whoopin like that", 
				"You wanna take me on next, tough guy?!", "Why dont you guys shake hands", "Is that necessary?", 
				"You guys are so immature!", "Never back down!", "No retreat, no surrender!", "Come on guys, hug it out", 
				"Fighting won't solve anything!", "Let them fight.", "Ok, break it up!", "Somebody stop him!", "You're hurting him, stop",
				"They fight like old people", "I'll fight the winner", "What's the first rule of fight club?", "That's ice cold bro!", 
				"Whoop his ***!", "That dude is pretty tough!", "You guys are going to get in trouble", "You shouldn't put your hands on someone",
				"Fighting is for losers!", "The horror!", "Bro, you need karate lessons!"} },

			{"Fidget", new string[] {
				"Looks like some kind of fancy microwave.",	"I Wonder if I should push this?", "This must be one of those smart toilets",
				"This is mines now! I'm keeping this.",	"This would look nicely in my apartment", "Well, lets find out", "Is this thing safe?",
				"This hunk of junk dont even work right", "This better be worth my time.", "Who built this?", "Whats going to happen?",	} },

			{"grabbed", new string[] {
				"Let go!", "Ahhh! Let go you b****!", "Stop doing that!", "Get the **** off me!", "Knock it off, ****", "Quit it!",	} },

			{"choke", new string[] {
				"Just tap out, man!", "Go to sleep, punk ass!", "Night night", "Lights out for you", "I'll choke you out, sucka", "Good night",	
				"Sweet dreams", "You can't escape my iron grip", "Hey, watch me choke this b**** out!", "Say \"Uncle\""} },
		};

		public static string GetRandomSmack( string category )
		{
			if ( !SmackText.ContainsKey( category ) ) return "";
			if ( !SmackList.ContainsKey( category ) || SmackList[category].Count == 0 ) SetCategory( category );

			string text = SmackList[category][0];
			SmackList[category].RemoveAt(0);

			return text;
		}

		public static void SetCategory( string category )
		{
			SmackList[ category ] = new List<string>();
			xxx.Shuffle<string>( SmackText[category] );
			SmackList[ category ].AddRange(SmackText[category]);
		}

		public bool IsFlipped => (NPC.Facing < 0.0f);

		void FixedUpdate()
		{
			if (!IsActive) return;
			
			if (IsFlipped != _lastFlipped) { 

				if ( IsFlipped )
				{
				   // ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Left;
					Offset = new Vector3(1f,2.0f);
				}
				else
				{
					//ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Right;
					Offset = new Vector3(-1f,1.3f);
				}

				_lastFlipped = IsFlipped;

			}
			if ( !NPC || !NPC.Head )
			{
				IsActive = false;
				Quiet();
				return;
			}
		   
			if (NPC && NPC.PBO)
				ChatBubble.transform.position = Vector3.Slerp(ChatBubble.transform.position,NPC.Head.position + Offset, Time.fixedDeltaTime);
		}

		void Start()
		{
			ChatBubble                      = new GameObject("chatbubble", typeof(TextMeshPro)) as GameObject;
		   if ( IsFlipped )
			{
				// ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Left;
				Offset = new Vector3(1f,1f);
			}
			else
			{
				//ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Right;
				Offset = new Vector3(-1f,1f);
			}

			ChatBubble.transform.position   = transform.position + Offset;
			ChatBubble.transform.rotation   = new Quaternion(0,0,0,0);

			if ( ChatBubble.gameObject.TryGetComponent<RectTransform>( out RectTransform rectImage ) )
			{
				rectImage.localScale = new Vector3(1,1);
				rectImage.transform.position = transform.position + Offset;
				rectImage.transform.rotation = new Quaternion(0,0,0,0);
			}

			GameObject ChatBG = new GameObject("ChatBG", typeof(SpriteRenderer)) as GameObject;
			
			SR = ChatBG.GetComponent<SpriteRenderer>();


		}

		public void Say( string text, float seconds, NpcBehaviour npc )
		{
			NPC     = npc;
			Message = text;
			Timer   = seconds;

			if ( IsFlipped )
			{
				Offset = new Vector3(1f,2.0f);
			}
			else
			{
				Offset = new Vector3(-1f,1.3f);
			}

			StartCoroutine(ISetup());

		}


		IEnumerator ISetup()
		{
			yield return new WaitForSeconds(0.1f);
			ChatBubble.transform.position       = NPC.Head.position + Offset;
			ChatMessage                         = ChatBubble.gameObject.GetOrAddComponent<TextMeshPro>();
			ChatMessage.autoSizeTextContainer   = true;
			ChatMessage.alignment               = TextAlignmentOptions.Left;
			ChatMessage.enableAutoSizing        = false;
			ChatMessage.fontSize                = 1.5f;
			ChatMessage.color                   = NPC.ChatColor;
			ChatMessage.extraPadding = true;
			ChatMessage.text                    = Message;
			
			yield return new WaitForFixedUpdate();
			SR.transform.SetParent(ChatBubble.gameObject.transform, false);
			
			Color c     = Color.black;
			c.a         = 0.5f;
			SR.drawMode = SpriteDrawMode.Sliced;
			SR.sprite   = NpcMain.ChatSpriteBG;
			SR.size     = ChatMessage.textBounds.size * 1.1f;
			SR.color    = c;
			IsActive    = true;
			

			yield return new WaitForSeconds(Timer);

			Quiet();
		}
		
		void OnDestroy()
		{
			ChatMessage.text = "";
			UnityEngine.Object.Destroy(ChatBubble);
			//UnityEngine.Object.Destroy(ChatMessage.gameObject);
		}
		

		public void Quiet()
		{
			IsActive         = false;
			ChatMessage.text = "";
			UnityEngine.Object.Destroy(ChatBubble);
			UnityEngine.Object.Destroy(ChatMessage.gameObject);
			UnityEngine.Object.Destroy(this);

		}
	}
}