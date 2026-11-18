using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RusthollowDemo : MonoBehaviour
{
    private enum WeaponType
    {
        Rifle,
        Pistol
    }

    private sealed class Actor
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
        public SpriteRenderer HealthBack;
        public SpriteRenderer HealthFill;
        public Vector2 Facing = Vector2.right;
        public float Health;
        public float AttackTimer;
        public float WakeTimer;
        public float AnimationTime;
        public float ActionTime;
        public float ActionDuration;
        public bool IsRaider;
        public bool IsPlayer;
        public bool IsRunning;
        public bool IsMeleeAttacking;
    }

    private sealed class Projectile
    {
        public GameObject Root;
        public Vector2 Position;
        public Vector2 Direction;
        public float Remaining;
        public float Damage;
    }

    private sealed class Effect
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
        public float Remaining;
        public float Duration;
        public float InitialScale;
    }

    private readonly List<Actor> raiders = new List<Actor>();
    private readonly List<Projectile> projectiles = new List<Projectile>();
    private readonly List<Effect> effects = new List<Effect>();
    private readonly HashSet<Collider2D> furnitureColliders = new HashSet<Collider2D>();
    private readonly List<UnityEngine.Object> generatedAssets = new List<UnityEngine.Object>();
    private readonly List<Sprite> operatorFrames = new List<Sprite>();
    private readonly List<Sprite> raiderFrames = new List<Sprite>();
    private readonly List<Sprite> operatorBackFrames = new List<Sprite>();
    private readonly List<Sprite> operatorSideFrames = new List<Sprite>();
    private readonly List<Sprite> operatorBackDiagonalFrames = new List<Sprite>();
    private readonly List<Sprite> operatorFrontDiagonalFrames = new List<Sprite>();
    private readonly List<Sprite> raiderBackFrames = new List<Sprite>();
    private readonly List<Sprite> raiderSideFrames = new List<Sprite>();
    private readonly List<Sprite> raiderBackDiagonalFrames = new List<Sprite>();
    private readonly List<Sprite> raiderFrontDiagonalFrames = new List<Sprite>();
    private readonly List<Sprite> rifleFrames = new List<Sprite>();
    private readonly List<Sprite> pistolFrames = new List<Sprite>();
    private readonly List<Sprite> playerMeleeFrames = new List<Sprite>();
    private readonly List<Sprite> raiderMeleeFrames = new List<Sprite>();
    private readonly Vector2[] startingRaiderPositions =
    {
        new Vector2(-5.2f, -0.25f),
        new Vector2(0f, 1.65f),
        new Vector2(5.2f, -0.25f),
        new Vector2(-4f, -1.25f),
        new Vector2(4f, -1.25f)
    };

    private Camera gameCamera;
    private Actor player;
    private Actor fixer;
    private Sprite[] propSprites;
    private Sprite mechanicSprite;
    private Sprite bulletSprite;
    private Sprite impactSprite;
    private Sprite solidSprite;
    private Vector2 aimPoint;
    private float fireCooldown;
    private float focusRemaining;
    private float focusCooldown;
    private float boostCooldown;
    private float boostRemaining;
    private float health = 100f;
    private int kills;
    private int credits = 120;
    private int xp;
    private WeaponType currentWeapon = WeaponType.Rifle;
    private bool fixerMessageVisible;
    private float fixerMessageTime;
    private string fixerMessage = "Mara: Keep the Rusted Halo standing.";
    private bool gameOver;

    private const float RoomHalfWidth = 8.2f;
    private const float RoomHalfDepth = 3.4f;
    private const int ContractGoal = 5;
    private const int WalkPoseStart = 1;
    private const int RunPoseStart = 5;
    private const int ShootPoseStart = 9;
    private const int AttackPose = 11;
    private static readonly Vector2 PlayerSpawnPosition = new Vector2(0f, -1.05f);

    private void Start()
    {
        gameCamera = GetComponent<Camera>();
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (gameCamera == null)
        {
            Debug.LogError("RusthollowDemo requires a Main Camera.");
            enabled = false;
            return;
        }

        if (!LoadArt())
        {
            enabled = false;
            return;
        }

        ConfigureCamera();
        CreateRoom();
        CreateFurniture();
        CreateActors();
        aimPoint = player.Root.transform.position;
    }

    private bool LoadArt()
    {
        Texture2D roomTexture = LoadTexture("room-isometric");
        Texture2D propTexture = LoadTexture("props");
        Texture2D peopleTexture = LoadTexture("people-gemini");
        Texture2D mechanicTexture = LoadTexture("mechanic");
        if (roomTexture == null || propTexture == null || peopleTexture == null || mechanicTexture == null)
        {
            return false;
        }

        Sprite roomSprite = MakeSprite(roomTexture, new Rect(0f, 0f, roomTexture.width, roomTexture.height), new Vector2(0.5f, 0.5f));
        CreatePropSprites(propTexture);
        CreateCharacterSprites(peopleTexture);
        mechanicSprite = MakeSprite(
            mechanicTexture,
            new Rect(0f, 0f, mechanicTexture.width, mechanicTexture.height),
            new Vector2(0.5f, 0.04f));

        Texture2D effectTexture = CreateEffectTexture();
        generatedAssets.Add(effectTexture);
        bulletSprite = MakeSprite(effectTexture, new Rect(0f, 0f, effectTexture.width, effectTexture.height), new Vector2(0.5f, 0.5f));
        impactSprite = MakeSprite(effectTexture, new Rect(0f, 0f, effectTexture.width, effectTexture.height), new Vector2(0.5f, 0.5f));
        Texture2D solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        solidTexture.name = "Rusthollow Solid Pixel";
        solidTexture.SetPixel(0, 0, Color.white);
        solidTexture.Apply();
        generatedAssets.Add(solidTexture);
        solidSprite = MakeSprite(solidTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        GameObject room = new GameObject("The Rusted Halo - illustrated room");
        room.transform.position = new Vector3(0f, 0f, 2f);
        SpriteRenderer roomRenderer = room.AddComponent<SpriteRenderer>();
        roomRenderer.sprite = roomSprite;
        roomRenderer.sortingOrder = -1000;
        return true;
    }

    private static Texture2D LoadTexture(string assetName)
    {
        Texture2D texture = Resources.Load<Texture2D>("RusthollowArt/" + assetName);
        if (texture == null)
        {
            Debug.LogError("Missing Rusthollow art resource: Resources/RusthollowArt/" + assetName + ".png");
            return null;
        }

        texture.filterMode = assetName == "people-gemini"
            ? FilterMode.Point
            : FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private Sprite MakeSprite(Texture2D texture, Rect rect, Vector2 pivot)
    {
        Sprite sprite = Sprite.Create(texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name + " sprite";
        generatedAssets.Add(sprite);
        return sprite;
    }

    private void CreatePropSprites(Texture2D texture)
    {
        propSprites = new Sprite[8];
        const int frameWidth = 240;
        const int frameHeight = 192;
        for (int i = 0; i < propSprites.Length; i++)
        {
            int column = i % 4;
            int rowFromTop = i / 4;
            Rect rect = new Rect(
                column * frameWidth,
                texture.height - (rowFromTop + 1) * frameHeight,
                frameWidth,
                frameHeight);
            propSprites[i] = MakeSprite(texture, rect, new Vector2(0.5f, 0.08f));
        }
    }

    private void CreateCharacterSprites(Texture2D texture)
    {
        const int frameWidth = 192;
        const int frameHeight = 256;
        var rows = new List<Sprite>[16];
        for (int rowFromTop = 0; rowFromTop < rows.Length; rowFromTop++)
        {
            rows[rowFromTop] = new List<Sprite>(4);
            for (int column = 0; column < 4; column++)
            {
                Rect rect = new Rect(
                    column * frameWidth,
                    texture.height - (rowFromTop + 1) * frameHeight,
                    frameWidth,
                    frameHeight);
                rows[rowFromTop].Add(MakeSprite(texture, rect, new Vector2(0.5f, 0.035f)));
            }
        }

        FillWalkingPoses(operatorFrames, rows[0]);
        FillWalkingPoses(operatorFrontDiagonalFrames, rows[1]);
        FillWalkingPoses(operatorBackDiagonalFrames, rows[2]);
        FillWalkingPoses(operatorSideFrames, rows[3]);
        FillWalkingPoses(operatorBackFrames, rows[4]);
        FillWalkingPoses(raiderFrames, rows[5]);
        FillWalkingPoses(raiderFrontDiagonalFrames, rows[6]);
        FillWalkingPoses(raiderBackDiagonalFrames, rows[7]);
        FillWalkingPoses(raiderSideFrames, rows[8]);
        FillWalkingPoses(raiderBackFrames, rows[9]);
        rifleFrames.AddRange(rows[10]);
        pistolFrames.AddRange(rows[12]);
        playerMeleeFrames.AddRange(rows[14]);
        raiderMeleeFrames.AddRange(rows[15]);
    }

    private static void FillWalkingPoses(List<Sprite> destination, List<Sprite> walkFrames)
    {
        destination.Clear();
        destination.Add(walkFrames[0]);
        destination.AddRange(walkFrames);
        destination.AddRange(walkFrames);
        destination.Add(walkFrames[0]);
        destination.Add(walkFrames[0]);
        destination.Add(walkFrames[0]);
    }

    private static Texture2D CreateEffectTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Rusthollow muzzle and impact glow";
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                byte alpha = (byte)(Mathf.Clamp01((1f - distance) * 2.6f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private void ConfigureCamera()
    {
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 5.4f;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.055f, 0.065f, 0.063f);
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.transform.rotation = Quaternion.identity;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void CreateRoom()
    {
        RenderSettings.fog = false;
        Time.timeScale = 1f;
    }

    private void CreateFurniture()
    {
        CreateProp("Backbar bottles", 4, new Vector2(-5.7f, 1.15f), new Vector2(1.8f, 1.44f), Vector2.zero, 20f);
        CreateProp("The Rusted Halo bar", 0, new Vector2(-5.45f, 0.05f), new Vector2(2.6f, 1.95f), new Vector2(1.7f, 0.58f), 20f);
        CreateProp("Salvage workbench", 1, new Vector2(-3.3f, 0.85f), new Vector2(2.2f, 1.72f), new Vector2(1.5f, 0.5f), 20f);
        CreateProp("Bounty terminal", 2, new Vector2(5.45f, 0.85f), new Vector2(1.8f, 1.44f), new Vector2(0.86f, 0.65f), -20f);
        CreateProp("Card table", 3, new Vector2(0f, 0.2f), new Vector2(1.8f, 1.5f), new Vector2(1.1f, 0.58f));
        CreateProp("Battered cot", 5, new Vector2(3.25f, 0.95f), new Vector2(2f, 1.6f), new Vector2(1.45f, 0.55f), -20f);
        CreateProp("Salvage crates", 6, new Vector2(4.4f, -1.1f), new Vector2(1.9f, 1.52f), new Vector2(1.25f, 0.64f), 20f);
        CreateProp("Bentwood chair", 7, new Vector2(-0.1f, -2.25f), new Vector2(1.3f, 1.36f), new Vector2(0.72f, 0.52f));
    }

    private void CreateProp(string name, int spriteIndex, Vector2 position, Vector2 size, Vector2 colliderSize, float rotation = 0f)
    {
        const float propWidth = 2.4f;
        const float propHeight = 1.92f;
        Vector3 scale = new Vector3(size.x / propWidth, size.y / propHeight, 1f);
        GameObject prop = new GameObject(name);
        prop.transform.position = new Vector3(position.x, position.y, 0f);
        prop.transform.localScale = scale;
        prop.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
        renderer.sprite = propSprites[spriteIndex];
        renderer.sortingOrder = SortOrder(position.y) + 1;
        if (colliderSize.sqrMagnitude > 0f)
        {
            BoxCollider2D collider = prop.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(colliderSize.x / scale.x, colliderSize.y / scale.y);
            collider.offset = new Vector2(0f, collider.size.y * 0.48f);
            collider.isTrigger = true;
            furnitureColliders.Add(collider);
        }
    }

    private void CreateActors()
    {
        player = CreateActor("Rustline Operator", PlayerSpawnPosition, true, false);
        player.Health = 100f;
        player.IsPlayer = true;
        player.Facing = Vector2.up;

        fixer = CreateActor("Mara - salvage mechanic", new Vector2(-4.2f, 0f), false, false);
        fixer.Renderer.sprite = mechanicSprite;
        fixer.Renderer.color = Color.white;
        fixer.Facing = ActorPosition(player) - ActorPosition(fixer);

        SpawnRaiders();
    }

    private void SpawnRaiders()
    {
        foreach (Vector2 position in startingRaiderPositions)
        {
            Actor raider = CreateActor("Rusthollow Raider", position, false, true);
            raider.Health = 56f;
            raider.AttackTimer = Random.Range(1.35f, 2.2f);
            raider.WakeTimer = 3.5f;
            SetFacing(raider, ActorPosition(player) - position);
            raiders.Add(raider);
        }
    }

    private Actor CreateActor(string name, Vector2 position, bool isPlayer, bool isRaider)
    {
        GameObject root = new GameObject(name);
        root.transform.position = new Vector3(position.x, position.y, -0.2f);
        root.transform.localScale = Vector3.one * 0.56f;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = (isRaider ? raiderFrames : operatorFrames)[0];
        renderer.color = isRaider ? new Color(1f, 0.58f, 0.43f) : Color.white;
        renderer.sortingOrder = SortOrder(position.y);

        Actor actor = new Actor
        {
            Root = root,
            Renderer = renderer,
            IsRaider = isRaider,
            IsPlayer = isPlayer
        };
        if (isRaider)
        {
            GameObject barBack = new GameObject(name + " health background");
            barBack.transform.SetParent(root.transform, false);
            barBack.transform.localPosition = new Vector3(0f, 2.3f, -0.01f);
            barBack.transform.localScale = new Vector3(0.72f, 0.08f, 1f);
            actor.HealthBack = barBack.AddComponent<SpriteRenderer>();
            actor.HealthBack.sprite = solidSprite;
            actor.HealthBack.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);
            actor.HealthBack.sortingOrder = 20;

            GameObject barFill = new GameObject(name + " health");
            barFill.transform.SetParent(barBack.transform, false);
            barFill.transform.localPosition = new Vector3(-0.5f, 0f, -0.01f);
            barFill.transform.localScale = new Vector3(1f, 0.72f, 1f);
            actor.HealthFill = barFill.AddComponent<SpriteRenderer>();
            actor.HealthFill.sprite = solidSprite;
            actor.HealthFill.color = new Color(0.82f, 0.24f, 0.15f);
            actor.HealthFill.sortingOrder = 21;
        }

        return actor;
    }

    private void Update()
    {
        if (player == null || player.Root == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        fireCooldown = Mathf.Max(0f, fireCooldown - deltaTime);
        focusCooldown = Mathf.Max(0f, focusCooldown - deltaTime);
        boostCooldown = Mathf.Max(0f, boostCooldown - deltaTime);
        boostRemaining = Mathf.Max(0f, boostRemaining - deltaTime);
        fixerMessageTime = Mathf.Max(0f, fixerMessageTime - deltaTime);
        fixerMessageVisible = fixerMessageTime > 0f;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && gameOver)
        {
            ResetPlayer();
        }

        if (gameOver)
        {
            UpdateEffects(deltaTime);
            return;
        }

        if (focusRemaining > 0f)
        {
            focusRemaining -= deltaTime;
            if (focusRemaining <= 0f)
            {
                focusRemaining = 0f;
                Time.timeScale = 1f;
            }
        }

        UpdateAimPoint(mouse);
        ReadPlayerInput(keyboard, mouse, deltaTime);
        UpdateRaiders(deltaTime);
        UpdateProjectiles(deltaTime);
        UpdateEffects(deltaTime);
        UpdateActors(deltaTime);

        if (health <= 0f)
        {
            health = 0f;
            gameOver = true;
            Time.timeScale = 1f;
        }
    }

    private void UpdateAimPoint(Mouse mouse)
    {
        if (mouse == null)
        {
            return;
        }

        Vector2 pointer = mouse.position.ReadValue();
        Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -gameCamera.transform.position.z));
        aimPoint = new Vector2(world.x, world.y);
    }

    private void ReadPlayerInput(Keyboard keyboard, Mouse mouse, float deltaTime)
    {
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            currentWeapon = WeaponType.Rifle;
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            currentWeapon = WeaponType.Pistol;
        }

        if (keyboard.qKey.wasPressedThisFrame && focusCooldown <= 0f)
        {
            focusRemaining = 2.3f;
            focusCooldown = 6f;
            Time.timeScale = 0.55f;
        }

        if (keyboard.eKey.wasPressedThisFrame && boostCooldown <= 0f)
        {
            boostRemaining = 3.5f;
            boostCooldown = 8f;
            health = Mathf.Min(100f, health + 26f);
            CreateEffect(ActorPosition(player) + Vector2.up * 0.55f, new Color(0.3f, 0.84f, 1f), 0.42f, 0.32f);
            ShowMessage("BIO-BOOST // tissue knit active");
        }

        if (keyboard.fKey.wasPressedThisFrame && Vector2.Distance(ActorPosition(player), ActorPosition(fixer)) < 2f)
        {
            fixerMessage = "Mara: Five raiders are tearing up my bar. Clear them and the bounty is yours.";
            fixerMessageTime = 5f;
        }

        Vector2 inputDirection = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDirection.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDirection.x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDirection.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDirection.y -= 1f;
        inputDirection = Vector2.ClampMagnitude(inputDirection, 1f);
        Vector2 movement = new Vector2(
            inputDirection.x - inputDirection.y,
            (inputDirection.x + inputDirection.y) * 0.5f);
        movement = Vector2.ClampMagnitude(movement, 1f);

        player.IsRunning = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (movement.sqrMagnitude > 0.001f)
        {
            SetFacing(player, movement);
            float speed = player.IsRunning ? 4.1f : 2.7f;
            MovePlayer(movement * speed * deltaTime);
            player.AnimationTime += deltaTime;
        }

        if (mouse != null && mouse.leftButton.isPressed && fireCooldown <= 0f)
        {
            FireCurrentWeapon();
        }

        if ((keyboard.spaceKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame)))
        {
            MeleeAttack();
        }
    }

    private void MovePlayer(Vector2 delta)
    {
        Vector2 position = ActorPosition(player);
        TryMove(player, position + Vector2.right * delta.x);
        position = ActorPosition(player);
        TryMove(player, position + Vector2.up * delta.y);
    }

    private void TryMove(Actor actor, Vector2 position)
    {
        if (!IsInsideRoom(position))
        {
            return;
        }

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, 0.22f);
        foreach (Collider2D overlap in overlaps)
        {
            if (furnitureColliders.Contains(overlap))
            {
                return;
            }
        }

        actor.Root.transform.position = new Vector3(position.x, position.y, actor.Root.transform.position.z);
    }

    private static bool IsInsideRoom(Vector2 position)
    {
        return Mathf.Abs(position.x) / RoomHalfWidth + Mathf.Abs(position.y) / RoomHalfDepth <= 1f;
    }

    private void FireCurrentWeapon()
    {
        bool isRifle = currentWeapon == WeaponType.Rifle;
        fireCooldown = isRifle ? 0.28f : 0.18f;
        player.ActionTime = isRifle ? 0.22f : 0.18f;
        player.ActionDuration = player.ActionTime;
        player.IsMeleeAttacking = false;
        Vector2 aimDirection = aimPoint - ActorPosition(player);
        if (aimDirection.sqrMagnitude > 0.01f)
        {
            SetFacing(player, aimDirection.normalized);
        }

        Vector2 origin = ActorPosition(player) + Vector2.up * 0.78f + player.Facing * 0.2f;
        Vector2 direction = aimPoint - origin;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = player.Facing;
        }

        direction.Normalize();
        GameObject bulletObject = new GameObject(isRifle ? "Rustline Rifle projectile" : "Sidearm Pistol projectile");
        bulletObject.transform.position = new Vector3(origin.x, origin.y, -0.5f);
        bulletObject.transform.right = new Vector3(direction.x, direction.y, 0f);
        bulletObject.transform.localScale = new Vector3(0.32f, 0.105f, 1f);
        SpriteRenderer renderer = bulletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = bulletSprite;
        renderer.color = isRifle ? new Color(1f, 0.83f, 0.43f) : new Color(0.82f, 0.9f, 1f);
        renderer.sortingOrder = 8000;

        projectiles.Add(new Projectile
        {
            Root = bulletObject,
            Position = origin,
            Direction = direction,
            Remaining = 1.2f,
            Damage = boostRemaining > 0f ? 38f : isRifle ? 28f : 18f
        });
    }

    private void MeleeAttack()
    {
        player.ActionTime = 0.38f;
        player.ActionDuration = player.ActionTime;
        player.IsMeleeAttacking = true;
        Vector2 origin = ActorPosition(player);
        bool hit = false;
        for (int i = raiders.Count - 1; i >= 0; i--)
        {
            Actor raider = raiders[i];
            Vector2 offset = ActorPosition(raider) - origin;
            if (offset.magnitude > 1.35f || Vector2.Dot(player.Facing, offset.normalized) < -0.1f)
            {
                continue;
            }

            DamageRaider(raider, 24f, ActorPosition(raider));
            hit = true;
        }

        Vector2 effectPosition = origin + player.Facing * 0.75f + Vector2.up * 0.8f;
        CreateEffect(effectPosition, hit ? new Color(1f, 0.76f, 0.36f) : new Color(0.72f, 0.81f, 0.78f), 0.52f, 0.18f);
    }

    private void UpdateRaiders(float deltaTime)
    {
        Vector2 playerPosition = ActorPosition(player);
        for (int i = raiders.Count - 1; i >= 0; i--)
        {
            Actor raider = raiders[i];
            if (raider.Root == null)
            {
                raiders.RemoveAt(i);
                continue;
            }

            if (raider.WakeTimer > 0f)
            {
                raider.WakeTimer -= deltaTime;
                continue;
            }

            Vector2 position = ActorPosition(raider);
            Vector2 toPlayer = playerPosition - position;
            float distance = toPlayer.magnitude;
            if (distance > 1.45f)
            {
                Vector2 direction = toPlayer.normalized;
                foreach (Actor other in raiders)
                {
                    if (other == raider)
                    {
                        continue;
                    }

                    Vector2 separation = position - ActorPosition(other);
                    float separationDistance = separation.magnitude;
                    if (separationDistance < 1.7f && separationDistance > 0.001f)
                    {
                        direction += separation / separationDistance * ((1.7f - separationDistance) * 2f);
                    }
                }

                direction.Normalize();
                SetFacing(raider, direction);
                float speed = 1.1f;
                if (!TryMoveRaider(raider, position + direction * speed * deltaTime))
                {
                    Vector2 sidestep = new Vector2(-direction.y, direction.x);
                    if (!TryMoveRaider(raider, position + sidestep * speed * deltaTime))
                    {
                        TryMoveRaider(raider, position - sidestep * speed * deltaTime);
                    }
                }

                raider.AnimationTime += deltaTime;
            }
            else
            {
                raider.AttackTimer -= deltaTime;
                if (raider.AttackTimer <= 0f)
                {
                    raider.AttackTimer = 1.65f;
                    raider.ActionTime = 0.52f;
                    raider.ActionDuration = raider.ActionTime;
                    raider.IsMeleeAttacking = true;
                    health = Mathf.Max(0f, health - 5.5f);
                    CreateEffect(playerPosition + Vector2.up * 0.45f, new Color(1f, 0.26f, 0.16f), 0.36f, 0.24f);
                }
            }
        }
    }

    private bool TryMoveRaider(Actor raider, Vector2 position)
    {
        if (!IsInsideRoom(position))
        {
            return false;
        }

        foreach (Actor other in raiders)
        {
            if (other != raider && Vector2.Distance(position, ActorPosition(other)) < 0.9f)
            {
                return false;
            }
        }

        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, 0.23f))
        {
            if (furnitureColliders.Contains(overlap))
            {
                return false;
            }
        }

        raider.Root.transform.position = new Vector3(position.x, position.y, raider.Root.transform.position.z);
        return true;
    }

    private void UpdateProjectiles(float deltaTime)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = projectiles[i];
            Vector2 previous = projectile.Position;
            projectile.Position += projectile.Direction * 17f * deltaTime;
            projectile.Remaining -= deltaTime;
            projectile.Root.transform.position = new Vector3(projectile.Position.x, projectile.Position.y, -0.5f);

            RaycastHit2D blocker = Physics2D.Linecast(previous, projectile.Position);
            if (blocker.collider != null && furnitureColliders.Contains(blocker.collider))
            {
                CreateEffect(blocker.point, new Color(1f, 0.78f, 0.42f), 0.28f, 0.16f);
                Destroy(projectile.Root);
                projectiles.RemoveAt(i);
                continue;
            }

            Actor target = null;
            foreach (Actor raider in raiders)
            {
                Vector2 targetPosition = ActorPosition(raider) + Vector2.up * 0.72f;
                if (DistanceToSegment(targetPosition, previous, projectile.Position) < 0.43f)
                {
                    target = raider;
                    break;
                }
            }

            if (target != null)
            {
                DamageRaider(target, projectile.Damage, projectile.Position);
                Destroy(projectile.Root);
                projectiles.RemoveAt(i);
            }
            else if (projectile.Remaining <= 0f || !IsInsideRoom(projectile.Position))
            {
                Destroy(projectile.Root);
                projectiles.RemoveAt(i);
            }
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }

    private void DamageRaider(Actor raider, float damage, Vector2 impactPosition)
    {
        if (raider == null || !raiders.Contains(raider))
        {
            return;
        }

        raider.Health -= damage;
        raider.Renderer.color = Color.white;
        raider.ActionTime = 0.12f;
        raider.ActionDuration = raider.ActionTime;
        raider.IsMeleeAttacking = false;
        CreateEffect(impactPosition, new Color(1f, 0.62f, 0.3f), 0.34f, 0.2f);
        if (raider.Health <= 0f)
        {
            raiders.Remove(raider);
            Destroy(raider.Root);
            kills++;
            credits += 22;
            xp += 15;
            if (kills == ContractGoal)
            {
                credits += 100;
                ShowMessage("BOUNTY COMPLETE // +100 credits");
            }
        }
    }

    private void UpdateActors(float deltaTime)
    {
        UpdateActor(player, deltaTime);
        UpdateActor(fixer, deltaTime);
        foreach (Actor raider in raiders)
        {
            UpdateActor(raider, deltaTime);
            if (raider.HealthFill != null && raider.HealthBack != null)
            {
                float healthFraction = Mathf.Clamp01(raider.Health / 56f);
                raider.HealthFill.transform.localScale = new Vector3(healthFraction, 0.72f, 1f);
                raider.HealthFill.color = Color.Lerp(new Color(0.85f, 0.17f, 0.12f), new Color(0.92f, 0.7f, 0.3f), 1f - healthFraction);
            }
        }
    }

    private void UpdateActor(Actor actor, float deltaTime)
    {
        if (actor == null || actor.Root == null)
        {
            return;
        }

        actor.ActionTime = Mathf.Max(0f, actor.ActionTime - deltaTime);
        Vector2 position = ActorPosition(actor);
        actor.Renderer.sortingOrder = SortOrder(position.y);
        List<Sprite> facingFrames = GetFramesForFacing(actor);
        actor.Renderer.flipX = IsSideFacing(actor.Facing)
            ? actor.Facing.x > 0f
            : actor.Facing.x < 0f;
        if (actor.IsPlayer)
        {
            actor.Renderer.color = boostRemaining > 0f
                ? new Color(0.65f, 0.92f, 1f)
                : (actor.ActionTime > 0f && health < 40f ? new Color(1f, 0.65f, 0.6f) : Color.white);
        }
        else if (actor.IsRaider && actor.ActionTime <= 0f)
        {
            actor.Renderer.color = new Color(1f, 0.58f, 0.43f);
        }

        int pose;
        if (actor.ActionTime > 0f)
        {
            if (actor.IsPlayer)
            {
                pose = actor.IsMeleeAttacking
                    ? AttackPose
                    : ShootPoseStart + ((int)(Time.time * 12f) % 2);
            }
            else
            {
                pose = actor.IsRaider && actor.IsMeleeAttacking ? AttackPose : 0;
            }
        }
        else if (actor.IsPlayer && Mouse.current != null && Mouse.current.leftButton.isPressed && fireCooldown > 0f)
        {
            pose = ShootPoseStart + ((int)(Time.time * 12f) % 2);
        }
        else if (actor.IsPlayer && player.IsRunning && PlayerIsMoving())
        {
            pose = RunPoseStart + ((int)(actor.AnimationTime / 0.11f) % 4);
        }
        else if (actor.IsPlayer && PlayerIsMoving())
        {
            pose = WalkPoseStart + ((int)(actor.AnimationTime / 0.18f) % 4);
        }
        else if (actor.IsRaider && raiderIsMoving(actor))
        {
            pose = WalkPoseStart + ((int)(actor.AnimationTime / 0.18f) % 4);
        }
        else
        {
            pose = 0;
        }

        if (actor == fixer && !IsSideFacing(actor.Facing) && actor.Facing.y <= 0f)
        {
            actor.Renderer.sprite = mechanicSprite;
        }
        else if (actor.IsPlayer && pose >= ShootPoseStart && pose < AttackPose)
        {
            List<Sprite> weaponFrames = currentWeapon == WeaponType.Rifle ? rifleFrames : pistolFrames;
            actor.Renderer.sprite = weaponFrames[(pose - ShootPoseStart) % weaponFrames.Count];
        }
        else if ((actor.IsPlayer || actor.IsRaider) && pose == AttackPose && actor.ActionTime > 0f)
        {
            List<Sprite> actionFrames = actor.IsRaider ? raiderMeleeFrames : playerMeleeFrames;
            float duration = actor.ActionDuration > 0f ? actor.ActionDuration : actor.ActionTime;
            float progress = Mathf.Clamp01(1f - actor.ActionTime / duration);
            int frameIndex = Mathf.Min(Mathf.FloorToInt(progress * actionFrames.Count), actionFrames.Count - 1);
            actor.Renderer.sprite = actionFrames[frameIndex];
        }
        else
        {
            actor.Renderer.sprite = facingFrames[pose];
        }
        if (actor.IsRaider && actor.HealthBack != null)
        {
            actor.HealthBack.sortingOrder = actor.Renderer.sortingOrder + 20;
            actor.HealthFill.sortingOrder = actor.Renderer.sortingOrder + 21;
        }
    }

    private bool PlayerIsMoving()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.aKey.isPressed || keyboard.dKey.isPressed || keyboard.wKey.isPressed || keyboard.sKey.isPressed ||
                keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed ||
                keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed);
    }

    private List<Sprite> GetFramesForFacing(Actor actor)
    {
        bool isRaider = actor.IsRaider;
        if (IsSideFacing(actor.Facing))
        {
            return isRaider ? raiderSideFrames : operatorSideFrames;
        }

        if (IsDiagonalFacing(actor.Facing))
        {
            if (actor.Facing.y > 0f)
            {
                return isRaider ? raiderBackDiagonalFrames : operatorBackDiagonalFrames;
            }

            return isRaider ? raiderFrontDiagonalFrames : operatorFrontDiagonalFrames;
        }

        if (actor.Facing.y > 0f)
        {
            return isRaider ? raiderBackFrames : operatorBackFrames;
        }

        return isRaider ? raiderFrames : operatorFrames;
    }

    private static bool IsSideFacing(Vector2 facing)
    {
        return Mathf.Abs(facing.x) > Mathf.Abs(facing.y) * 2.2f;
    }

    private static bool IsDiagonalFacing(Vector2 facing)
    {
        float x = Mathf.Abs(facing.x);
        float y = Mathf.Abs(facing.y);
        return x > y * 0.45f && x <= y * 2.2f;
    }

    private bool raiderIsMoving(Actor actor)
    {
        return actor.WakeTimer <= 0f && Vector2.Distance(ActorPosition(actor), ActorPosition(player)) > 0.9f;
    }

    private void UpdateEffects(float deltaTime)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            Effect effect = effects[i];
            effect.Remaining -= deltaTime;
            if (effect.Remaining <= 0f)
            {
                Destroy(effect.Root);
                effects.RemoveAt(i);
                continue;
            }

            float progress = 1f - effect.Remaining / effect.Duration;
            float scale = effect.InitialScale * Mathf.Lerp(0.55f, 1.65f, progress);
            effect.Root.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = effect.Renderer.color;
            color.a = 1f - progress;
            effect.Renderer.color = color;
        }
    }

    private void CreateEffect(Vector2 position, Color color, float scale, float duration)
    {
        GameObject effectObject = new GameObject("Combat impact");
        effectObject.transform.position = new Vector3(position.x, position.y, -0.7f);
        effectObject.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = impactSprite;
        renderer.color = color;
        renderer.sortingOrder = 9000;
        effects.Add(new Effect
        {
            Root = effectObject,
            Renderer = renderer,
            Remaining = duration,
            Duration = duration,
            InitialScale = scale
        });
    }

    private void ResetPlayer()
    {
        player.Root.transform.position = new Vector3(PlayerSpawnPosition.x, PlayerSpawnPosition.y, player.Root.transform.position.z);
        player.Facing = Vector2.up;
        player.ActionTime = 0f;
        player.ActionDuration = 0f;
        player.IsMeleeAttacking = false;
        player.AnimationTime = 0f;
        player.IsRunning = false;
        currentWeapon = WeaponType.Rifle;
        foreach (Actor raider in raiders)
        {
            if (raider.Root != null)
            {
                Destroy(raider.Root);
            }
        }

        raiders.Clear();
        SpawnRaiders();
        foreach (Projectile projectile in projectiles)
        {
            Destroy(projectile.Root);
        }

        projectiles.Clear();
        foreach (Effect effect in effects)
        {
            Destroy(effect.Root);
        }

        effects.Clear();
        health = 100f;
        kills = 0;
        credits = 120;
        xp = 0;
        fireCooldown = 0f;
        focusRemaining = 0f;
        focusCooldown = 0f;
        boostRemaining = 0f;
        boostCooldown = 0f;
        Time.timeScale = 1f;
        gameOver = false;
    }

    private void ShowMessage(string message)
    {
        fixerMessage = message;
        fixerMessageTime = 2.5f;
    }

    private static Vector2 ActorPosition(Actor actor)
    {
        return actor == null || actor.Root == null ? Vector2.zero : actor.Root.transform.position;
    }

    private static int SortOrder(float y)
    {
        return Mathf.RoundToInt(-y * 100f);
    }

    private static void SetFacing(Actor actor, Vector2 facing)
    {
        actor.Facing = facing;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDestroy()
    {
        foreach (UnityEngine.Object generatedAsset in generatedAssets)
        {
            if (generatedAsset != null)
            {
                Destroy(generatedAsset);
            }
        }
    }

    private void OnGUI()
    {
        if (player == null || player.Root == null)
        {
            return;
        }

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.79f, 0.48f) }
        };
        GUIStyle heading = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.56f, 0.9f, 0.88f) }
        };
        GUIStyle body = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.88f, 0.87f, 0.79f) }
        };

        GUILayout.BeginArea(new Rect(16f, 16f, 360f, 210f), GUI.skin.box);
        GUILayout.Label("CHRYSALIS: ZERO", title);
        GUILayout.Label("THE RUSTED HALO  //  RUSTHOLLOW", heading);
        DrawMeter("HEALTH", health, 100f, new Color(0.35f, 0.82f, 0.58f));
        GUILayout.Space(4f);
        GUILayout.Label(kills >= ContractGoal
            ? "BOUNTY  COMPLETE    //    +100 CREDITS"
            : $"BOUNTY  Clear raiders    {Mathf.Min(kills, ContractGoal)}/{ContractGoal}", body);
        GUILayout.Label($"CREDITS  {credits}       XP  {xp}", body);
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 346f, 16f, 330f, 260f), GUI.skin.box);
        GUILayout.Label("FIELD CONTROLS", heading);
        GUILayout.Label("WASD / Arrows   Move", body);
        GUILayout.Label("Click Game view for keyboard focus", body);
        GUILayout.Label("Mouse   Aim", body);
        string weaponName = currentWeapon == WeaponType.Rifle ? "Rustline Rifle" : "Sidearm Pistol";
        GUILayout.Label($"Left click   Fire {weaponName}", body);
        GUILayout.Label("1   Rifle   |   2   Pistol", body);
        GUILayout.Label("Right click / Space   Melee", body);
        GUILayout.Label("Shift   Run   |   Q   Tactical Focus", body);
        GUILayout.Label("E   Bio-Boost   |   F   Talk to Mara", body);
        GUILayout.EndArea();

        if (fixer != null && fixer.Root != null)
        {
            Vector3 fixerScreen = gameCamera.WorldToScreenPoint(fixer.Root.transform.position + Vector3.up * 2.5f);
            if (fixerScreen.z > 0f)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.82f, 0.56f) }
                };
                GUI.Label(new Rect(fixerScreen.x - 100f, Screen.height - fixerScreen.y - 20f, 200f, 24f), "MARA  //  FIXER", nameStyle);
            }
        }

        if (fixerMessageVisible)
        {
            GUIStyle messageStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = new Color(1f, 0.9f, 0.72f) }
            };
            GUI.Label(new Rect(Screen.width * 0.28f, Screen.height - 98f, Screen.width * 0.44f, 42f), fixerMessage, messageStyle);
        }

        if (gameOver)
        {
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(Screen.width * 0.33f, Screen.height * 0.82f, Screen.width * 0.34f, 90f),
                "OPERATOR DOWN\nPress R to get back up", gameOverStyle);
        }

        DrawReticle();
    }

    private static void DrawMeter(string label, float value, float maximum, Color color)
    {
        GUILayout.Label($"{label}  {Mathf.CeilToInt(value)}/{Mathf.CeilToInt(maximum)}");
        Rect rect = GUILayoutUtility.GetRect(320f, 12f);
        GUI.color = new Color(0.04f, 0.05f, 0.05f, 0.9f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        rect.width *= maximum <= 0f ? 0f : Mathf.Clamp01(value / maximum);
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static void DrawReticle()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 pointer = mouse.position.ReadValue();
        float x = pointer.x;
        float y = Screen.height - pointer.y;
        GUI.color = new Color(1f, 0.78f, 0.43f, 0.95f);
        GUI.DrawTexture(new Rect(x - 9f, y - 1f, 18f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 1f, y - 9f, 2f, 18f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
