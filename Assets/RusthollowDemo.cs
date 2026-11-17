using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RusthollowDemo : MonoBehaviour
{
    private sealed class Raider
    {
        public GameObject Root;
        public Collider Collider;
        public float Health;
        public float AttackTimer;
    }

    private readonly List<Raider> raiders = new List<Raider>();
    private readonly Dictionary<Collider, Raider> raidersByCollider = new Dictionary<Collider, Raider>();
    private readonly List<GameObject> fuelCells = new List<GameObject>();
    private readonly int[] abilities = { 12, 10, 8, 11, 12 };
    private readonly string[] abilityNames =
    {
        "Ballistics & Arms",
        "Bio-Sync",
        "Cyber-Tech",
        "Tactics & Reflex",
        "Grit & Physique"
    };

    private Camera gameCamera;
    private CharacterController playerController;
    private Transform player;
    private Material groundMaterial;
    private Material scrapMaterial;
    private Material darkMetalMaterial;
    private Material raiderMaterial;
    private Material fuelMaterial;
    private Material playerMaterial;
    private Material tracerMaterial;
    private Vector3 playerVelocity;
    private Vector3 aimPoint;
    private int level = 1;
    private int xp;
    private int xpToNext = 60;
    private int skillPoints = 2;
    private int credits = 120;
    private int kills;
    private int fuelCollected;
    private int rifleProficiency = 1;
    private int proficiencyProgress;
    private float health = 100f;
    private float maxHealth = 100f;
    private float bioCalories = 100f;
    private float ghostBleed = 20f;
    private float fireCooldown;
    private float focusRemaining;
    private float focusCooldown;
    private float boostRemaining;
    private bool scrapyardComplete;
    private bool fuelContractComplete;
    private bool ambushStarted;
    private bool ambushComplete;
    private float ambushTime;
    private float ambushSpawnTimer;
    private float cameraYaw;
    private float cameraPitch = 13f;
    private const float AmbushDuration = 25f;

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

        CreateMaterials();
        CreateEnvironment();
        CreatePlayer();
        CreateFuelCells();
        SpawnStartingRaiders();
        ConfigureCamera();
        UpdateAimPoint();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        float unscaledDelta = Time.unscaledDeltaTime;
        fireCooldown = Mathf.Max(0f, fireCooldown - unscaledDelta);
        focusCooldown = Mathf.Max(0f, focusCooldown - unscaledDelta);

        if (focusRemaining > 0f)
        {
            focusRemaining -= unscaledDelta;
            if (focusRemaining <= 0f)
            {
                focusRemaining = 0f;
                Time.timeScale = 1f;
            }
        }

        if (boostRemaining > 0f)
        {
            boostRemaining = Mathf.Max(0f, boostRemaining - unscaledDelta);
        }

        ReadInput();
        UpdateRaiders();
        UpdateFuelCells();
        UpdateContracts(Time.deltaTime);
        UpdateVitals(Time.deltaTime);
        UpdateLevel();
    }

    private void LateUpdate()
    {
        if (player == null || gameCamera == null)
        {
            return;
        }

        Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 desiredPosition = player.position + Vector3.up * 1.35f + orbit * new Vector3(0f, 1.5f, -7.5f);
        gameCamera.transform.position = Vector3.Lerp(
            gameCamera.transform.position,
            desiredPosition,
            1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        Vector3 lookTarget = player.position + Vector3.up * 1.1f;
        gameCamera.transform.rotation = Quaternion.LookRotation(lookTarget - gameCamera.transform.position);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void CreateMaterials()
    {
        groundMaterial = MakeMaterial(new Color(0.18f, 0.20f, 0.19f));
        scrapMaterial = MakeMaterial(new Color(0.29f, 0.32f, 0.34f));
        darkMetalMaterial = MakeMaterial(new Color(0.10f, 0.14f, 0.17f));
        raiderMaterial = MakeMaterial(new Color(0.65f, 0.22f, 0.20f));
        fuelMaterial = MakeMaterial(new Color(0.12f, 0.83f, 0.90f));
        playerMaterial = MakeMaterial(new Color(0.14f, 0.66f, 0.78f));
        tracerMaterial = MakeMaterial(new Color(1f, 0.76f, 0.36f));
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        material.enableInstancing = true;
        return material;
    }

    private void CreateEnvironment()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.24f, 0.29f, 0.31f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.006f;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Rusthollow Silt Yard";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        CreateBlock("Scrap wall west", new Vector3(-25f, 1f, 0f), new Vector3(1f, 2f, 50f), darkMetalMaterial);
        CreateBlock("Scrap wall east", new Vector3(25f, 1f, 0f), new Vector3(1f, 2f, 50f), darkMetalMaterial);
        CreateBlock("Scrap wall north", new Vector3(0f, 1f, 25f), new Vector3(50f, 2f, 1f), darkMetalMaterial);
        CreateBlock("Scrap wall south", new Vector3(0f, 1f, -25f), new Vector3(50f, 2f, 1f), darkMetalMaterial);

        CreateBlock("Rust container A", new Vector3(-8f, 1f, 7f), new Vector3(3f, 2f, 8f), scrapMaterial);
        CreateBlock("Rust container B", new Vector3(10f, 1f, 10f), new Vector3(8f, 2f, 3f), scrapMaterial);
        CreateBlock("Broken gantry", new Vector3(-15f, 1f, -9f), new Vector3(3f, 2f, 9f), darkMetalMaterial);
        CreateBlock("Salvage stack", new Vector3(13f, 1f, -12f), new Vector3(7f, 2f, 3f), scrapMaterial);
        CreateBlock("Low cover", new Vector3(1f, 0.65f, -15f), new Vector3(9f, 1.3f, 2f), scrapMaterial);
    }

    private static void CreateBlock(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void CreatePlayer()
    {
        GameObject operatorObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        operatorObject.name = "Operator";
        operatorObject.layer = 2;
        operatorObject.transform.position = new Vector3(0f, 1f, 0f);
        operatorObject.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        operatorObject.GetComponent<CapsuleCollider>().enabled = false;
        playerController = operatorObject.AddComponent<CharacterController>();
        playerController.height = 2f;
        playerController.radius = 0.42f;
        playerController.center = Vector3.zero;
        playerController.slopeLimit = 45f;
        playerController.stepOffset = 0.3f;
        player = operatorObject.transform;

        GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifle.name = "Rustline Rifle";
        rifle.layer = 2;
        rifle.transform.SetParent(player, false);
        rifle.transform.localPosition = new Vector3(0.42f, 0.05f, 0.52f);
        rifle.transform.localScale = new Vector3(0.16f, 0.18f, 0.8f);
        rifle.GetComponent<Renderer>().sharedMaterial = darkMetalMaterial;
        rifle.GetComponent<Collider>().enabled = false;
    }

    private void CreateFuelCells()
    {
        Vector3[] positions =
        {
            new Vector3(7f, 0.55f, 2f),
            new Vector3(-17f, 0.55f, 11f),
            new Vector3(18f, 0.55f, -4f),
            new Vector3(-5f, 0.55f, -18f),
            new Vector3(4f, 0.55f, 18f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cell.name = "Fuel Cell " + (i + 1);
            cell.transform.position = positions[i];
            cell.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);
            cell.GetComponent<Renderer>().sharedMaterial = fuelMaterial;
            fuelCells.Add(cell);
        }
    }

    private void SpawnStartingRaiders()
    {
        Vector3[] positions =
        {
            new Vector3(-12f, 1f, 15f),
            new Vector3(13f, 1f, 15f),
            new Vector3(-18f, 1f, 4f),
            new Vector3(18f, 1f, -7f),
            new Vector3(9f, 1f, -18f),
            new Vector3(-13f, 1f, -17f)
        };

        foreach (Vector3 position in positions)
        {
            SpawnRaider(position);
        }
    }

    private void SpawnRaider(Vector3 position)
    {
        GameObject raiderObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        raiderObject.name = "Silt Raider";
        raiderObject.transform.position = position;
        raiderObject.GetComponent<Renderer>().sharedMaterial = raiderMaterial;
        Raider raider = new Raider
        {
            Root = raiderObject,
            Collider = raiderObject.GetComponent<Collider>(),
            Health = 60f,
            AttackTimer = Random.Range(0.5f, 1.3f)
        };
        raiders.Add(raider);
        raidersByCollider.Add(raider.Collider, raider);
    }

    private void ConfigureCamera()
    {
        gameCamera.fieldOfView = 58f;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.23f, 0.29f, 0.32f);
        cameraYaw = player.eulerAngles.y;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame && focusCooldown <= 0f && focusRemaining <= 0f)
        {
            focusRemaining = Mathf.Clamp(2f + (abilities[3] - 11) * 0.04f, 1.5f, 4f);
            focusCooldown = 5f;
            Time.timeScale = 0.7f;
        }

        if (keyboard.eKey.wasPressedThisFrame && boostRemaining <= 0f && bioCalories >= 25f)
        {
            bioCalories -= 25f;
            ghostBleed = Mathf.Min(100f, ghostBleed + 10f);
            boostRemaining = 4f;
        }

        Vector2 movement = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
        movement = Vector2.ClampMagnitude(movement, 1f);

        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 lookDelta = mouse.delta.ReadValue();
            cameraYaw += lookDelta.x * 0.12f;
            cameraPitch = Mathf.Clamp(cameraPitch - lookDelta.y * 0.08f, 5f, 35f);
        }

        Quaternion moveRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 moveDirection = moveRotation * new Vector3(movement.x, 0f, movement.y);
        float speed = 5f * (1f + Mathf.Max(0, abilities[3] - 10) * 0.025f);
        if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
        {
            speed *= 1.5f;
        }

        if (playerController.isGrounded && playerVelocity.y < 0f)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y -= 20f * Time.deltaTime;
        playerController.Move((moveDirection * speed + Vector3.up * playerVelocity.y) * Time.deltaTime);
        player.position = new Vector3(
            Mathf.Clamp(player.position.x, -23f, 23f),
            player.position.y,
            Mathf.Clamp(player.position.z, -23f, 23f));

        UpdateAimPoint();
        if (mouse != null && mouse.leftButton.isPressed && fireCooldown <= 0f)
        {
            FireRifle();
        }
    }

    private void UpdateAimPoint()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || gameCamera == null)
        {
            aimPoint = player.position + player.forward * 10f;
            return;
        }

        Ray aimRay = gameCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.up, Vector3.up * 0.9f);
        if (aimPlane.Raycast(aimRay, out float distance))
        {
            aimPoint = aimRay.GetPoint(distance);
        }
        else
        {
            aimPoint = player.position + player.forward * 10f;
        }

        Vector3 facing = aimPoint - player.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f)
        {
            player.rotation = Quaternion.LookRotation(facing);
        }
    }

    private void FireRifle()
    {
        fireCooldown = 0.2f;
        Vector3 muzzle = player.position + Vector3.up * 1.25f + player.forward * 0.55f;
        Vector3 direction = aimPoint - muzzle;
        if (direction.sqrMagnitude < 0.05f)
        {
            direction = player.forward;
        }

        Ray shot = new Ray(muzzle, direction.normalized);
        Vector3 end = muzzle + shot.direction * 80f;
        if (Physics.Raycast(shot, out RaycastHit hit, 80f, ~(1 << 2)))
        {
            end = hit.point;
            if (raidersByCollider.TryGetValue(hit.collider, out Raider raider))
            {
                float statScale = 1f + (abilities[0] - 10) * 0.025f;
                float equipmentScale = 1f + (rifleProficiency - 1) * 0.06f;
                float boostScale = boostRemaining > 0f ? 1.55f : 1f;
                raider.Health -= 22f * statScale * equipmentScale * boostScale;
                proficiencyProgress += 8;
                if (proficiencyProgress >= rifleProficiency * 80 && rifleProficiency < 5)
                {
                    proficiencyProgress = 0;
                    rifleProficiency++;
                }

                if (raider.Health <= 0f)
                {
                    KillRaider(raider);
                }
            }
        }

        CreateTracer(muzzle, end);
    }

    private void CreateTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObject = new GameObject("Rifle Tracer");
        LineRenderer line = tracerObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.025f;
        line.endWidth = 0.008f;
        line.sharedMaterial = tracerMaterial;
        line.startColor = Color.white;
        line.endColor = new Color(1f, 0.53f, 0.18f, 0.1f);
        Destroy(tracerObject, 0.06f);
    }

    private void KillRaider(Raider raider)
    {
        raidersByCollider.Remove(raider.Collider);
        raiders.Remove(raider);
        Destroy(raider.Root);
        kills++;
        credits += 18;
        xp += 15;
        if (kills >= 6)
        {
            scrapyardComplete = true;
        }
    }

    private void UpdateRaiders()
    {
        for (int i = raiders.Count - 1; i >= 0; i--)
        {
            Raider raider = raiders[i];
            if (raider.Root == null)
            {
                raidersByCollider.Remove(raider.Collider);
                raiders.RemoveAt(i);
                continue;
            }

            Vector3 toPlayer = player.position - raider.Root.transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance > 1.55f)
            {
                Vector3 direction = toPlayer.normalized;
                raider.Root.transform.position += direction * 2.2f * Time.deltaTime;
                raider.Root.transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                raider.AttackTimer -= Time.deltaTime;
                if (raider.AttackTimer <= 0f)
                {
                    float resistance = Mathf.Clamp((abilities[4] - 10) * 0.02f, 0f, 0.35f);
                    health = Mathf.Max(0f, health - 9f * (1f - resistance));
                    raider.AttackTimer = 1.25f;
                }
            }
        }
    }

    private void UpdateFuelCells()
    {
        for (int i = fuelCells.Count - 1; i >= 0; i--)
        {
            GameObject cell = fuelCells[i];
            if (cell == null)
            {
                fuelCells.RemoveAt(i);
                continue;
            }

            cell.transform.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            Vector3 cellPosition = cell.transform.position;
            Vector3 playerPosition = player.position;
            cellPosition.y = 0f;
            playerPosition.y = 0f;
            if (Vector3.Distance(cellPosition, playerPosition) < 1.3f)
            {
                fuelCells.RemoveAt(i);
                Destroy(cell);
                fuelCollected++;
                credits += 25;
                bioCalories = Mathf.Min(100f, bioCalories + 20f);
                if (fuelCollected >= 3)
                {
                    fuelContractComplete = true;
                }
            }
        }
    }

    private void UpdateContracts(float deltaTime)
    {
        if (!ambushStarted && (scrapyardComplete || fuelContractComplete))
        {
            ambushStarted = true;
            ambushTime = 0f;
            SpawnRaider(new Vector3(-16f, 1f, 18f));
            SpawnRaider(new Vector3(16f, 1f, 18f));
            SpawnRaider(new Vector3(0f, 1f, -20f));
        }

        if (!ambushStarted || ambushComplete)
        {
            return;
        }

        ambushTime += deltaTime;
        ambushSpawnTimer += deltaTime;
        if (ambushSpawnTimer >= 4f && raiders.Count < 9)
        {
            ambushSpawnTimer = 0f;
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(13f, 18f);
            SpawnRaider(new Vector3(
                Mathf.Clamp(player.position.x + offset.x, -21f, 21f),
                1f,
                Mathf.Clamp(player.position.z + offset.y, -21f, 21f)));
        }

        if (ambushTime >= AmbushDuration)
        {
            ambushComplete = true;
            credits += 100;
            xp += 50;
        }
    }

    private void UpdateVitals(float deltaTime)
    {
        maxHealth = 100f + Mathf.Max(0, abilities[4] - 12) * 4f;
        health = Mathf.Min(maxHealth, health + deltaTime * 1.2f);
        bioCalories = Mathf.Min(100f, bioCalories + deltaTime * 3.5f);

        if (health <= 0f)
        {
            health = maxHealth;
            playerController.enabled = false;
            player.position = new Vector3(0f, 1f, 0f);
            playerController.enabled = true;
            credits = Mathf.Max(0, credits - 15);
        }
    }

    private void UpdateLevel()
    {
        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            level++;
            skillPoints++;
            xpToNext = Mathf.RoundToInt(xpToNext * 1.45f);
        }
    }

    private int ImplantSlots => Mathf.Min(8, 2 + (level - 1) / 3);

    private void OnGUI()
    {
        if (player == null)
        {
            return;
        }

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.42f, 0.94f, 1f) }
        };
        GUIStyle section = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.78f, 0.87f, 0.95f) }
        };

        GUILayout.BeginArea(new Rect(16f, 16f, 355f, 565f), GUI.skin.box);
        GUILayout.Label("CHRYSALIS: ZERO  //  RUSTHOLLOW", title);
        GUILayout.Label($"Level {level}     XP {xp}/{xpToNext}     Credits {credits}");
        DrawMeter("Hull", health, maxHealth, new Color(0.35f, 0.9f, 0.62f));
        DrawMeter("Bio-Calories", bioCalories, 100f, new Color(0.32f, 0.75f, 1f));
        DrawMeter("Ghost-Bleed", ghostBleed, 100f, new Color(1f, 0.47f, 0.36f));
        GUILayout.Label($"Implant slots unlocked: {ImplantSlots}  ( +1 every 3 levels )");
        GUILayout.Label($"Rustline Rifle proficiency: {rifleProficiency}/5  |  +6% damage per rank");

        GUILayout.Space(5f);
        GUILayout.Label($"Core Abilities  |  Skill points: {skillPoints}", section);
        for (int i = 0; i < abilities.Length; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{abilityNames[i]}: {abilities[i]}", GUILayout.Width(235f));
            GUI.enabled = skillPoints > 0;
            if (GUILayout.Button("+1", GUILayout.Width(55f)))
            {
                abilities[i]++;
                skillPoints--;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(5f);
        GUILayout.Label("Bounty Terminal", section);
        DrawContract("Scrapyard Sweep", $"{Mathf.Min(kills, 6)}/6 raiders", scrapyardComplete);
        DrawContract("Fuel Cell Recovery", $"{Mathf.Min(fuelCollected, 3)}/3 cells", fuelContractComplete);
        string ambushProgress = !ambushStarted
            ? "Unlock by completing either contract above"
            : ambushComplete
                ? "Complete"
                : $"{Mathf.Min(AmbushDuration, ambushTime):0}/{AmbushDuration:0}s survived";
        DrawContract("Hold the Line", ambushProgress, ambushComplete);
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 300f, 16f, 284f, 142f), GUI.skin.box);
        GUILayout.Label("FIELD CONTROLS", section);
        GUILayout.Label("WASD / Arrows  Move");
        GUILayout.Label("Mouse  Aim  |  Left click  Fire");
        GUILayout.Label("Q  Tactical Focus (slow time)");
        GUILayout.Label("E  Bio-Boost  |  Shift  Sprint");
        GUILayout.Label("Right mouse drag  Orbit camera");
        GUILayout.EndArea();

        DrawReticle();
    }

    private static void DrawMeter(string label, float value, float maximum, Color color)
    {
        GUILayout.Label($"{label}: {Mathf.CeilToInt(value)}/{Mathf.CeilToInt(maximum)}");
        Rect rect = GUILayoutUtility.GetRect(300f, 10f);
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        rect.width *= maximum <= 0f ? 0f : Mathf.Clamp01(value / maximum);
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static void DrawContract(string title, string progress, bool complete)
    {
        GUI.color = complete ? new Color(0.55f, 1f, 0.68f) : Color.white;
        GUILayout.Label($"{(complete ? "[DONE]" : "[ACTIVE]")} {title}");
        GUI.color = new Color(0.68f, 0.78f, 0.88f);
        GUILayout.Label("    " + progress);
        GUI.color = Color.white;
    }

    private static void DrawReticle()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 pointerPosition = mouse.position.ReadValue();
        float x = pointerPosition.x;
        float y = Screen.height - pointerPosition.y;
        GUI.color = new Color(0.65f, 0.96f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(x - 9f, y - 1f, 18f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 1f, y - 9f, 2f, 18f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
