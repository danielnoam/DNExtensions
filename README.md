# DNExtensions

A comprehensive collection of Unity extensions, systems, components, and utilities to accelerate game development.

## Table of Contents

- [Components](#components)
- [Systems](#systems)
- [Utilities](#utilities)

---

## Components

General-purpose Unity components for common game functionality.

### Billboard
Automatically rotates an object to face the camera.

### FPSCounter
Displays frames per second in your game with customizable styling and update rates.

```csharp
// Add to a GameObject to display FPS
gameObject.AddComponent<FPSCounter>();
```

### FreeFormCameraController
A flexible camera controller for scene navigation and free-form camera movement.

### Note
Editor component for adding notes and reminders to GameObjects directly in the Inspector.

```csharp
// Visible in the Inspector - useful for leaving development reminders
```

### RadialLayoutGroup
Arranges UI elements in a circular/radial pattern. Perfect for radial menus, circular inventories, and more.

```csharp
// Add to a UI GameObject to arrange children in a circle
RadialLayoutGroup radialLayout = gameObject.AddComponent<RadialLayoutGroup>();
radialLayout.radius = 100f;
radialLayout.startAngle = 0f;
```

### TubeRenderer
Renders tubes/pipes along paths defined by transform points.

**Features:**
- Dynamic tube mesh generation
- Custom radius and segments
- Visual editor for path editing

**Usage:**
1. Add TubeRenderer component
2. Assign transform points to define the path
3. Configure tube radius and segments
4. The tube mesh updates automatically

---

## Systems

Complete gameplay systems ready to integrate into your projects.

### Controller Rumble System

A comprehensive system for managing gamepad haptic feedback.

**Components:**
- **ControllerRumbleEffect**: Defines rumble effects with curves and duration
- **ControllerRumbleListener**: Global manager for processing rumble effects
- **ControllerRumbleSource**: Trigger rumble effects from any GameObject
- **ControllerRumbleUI**: UI integration for rumble feedback

**Usage:**
```csharp
// Add a rumble source to your GameObject
ControllerRumbleSource rumbleSource = gameObject.AddComponent<ControllerRumbleSource>();

// Create and play a rumble effect
rumbleSource.Rumble(new ControllerRumbleEffectSettings(0.5f, 0.7f, 0.3f));
```

---

### First Person Controller

A complete first-person controller system with interaction support.

**Features:**
- **FPCCamera**: First-person camera with mouse/gamepad look, FOV changes
- **FPCInput**: Input handling using Unity's new Input System
- **FPCMovement**: Character movement with walking, running, jumping, coyote time, and jump buffering
- **FPCInteraction**: Object interaction and pickup system
- **FPCManager**: Centralized manager component
- **FPCRigidBodyPush**: Push rigidbodies when walking into them
- **Interactable System**: Base classes and interfaces for creating interactable objects
- **PickableObject**: Objects that can be picked up, held, and thrown

**Usage:**
1. Add `FpcManager` component to your player GameObject
2. Configure character controller settings
3. Create interactable objects by inheriting from `InteractableBase`

```csharp
// Example: Creating a custom interactable
public class MyDoor : InteractableBase
{
    public override void Interact(InteractorData interactorData)
    {
        base.Interact(interactorData);
        // Open door logic
    }
}
```

---

### Grid System

A flexible 2D grid system with customizable orientation and cell management.

**Features:**
- **Grid**: 2D grid with horizontal/vertical orientation
- **CoordinateConverter**: Convert between world space and grid coordinates
- **Grid Editor**: Visual grid editing in the Unity editor
- **SOGridShape**: Scriptable Object for storing grid shapes

**Usage:**
```csharp
// Create a grid
Grid grid = new Grid(8, 8);
grid.orientation = GridOrientation.Vertical;

// Get cell from world position
Vector2Int cell = grid.GetCell(worldPosition);

// Get world position from cell
Vector3 worldPos = grid.GetCellWorldPosition(cell);

// Activate/deactivate cells
grid.ActivateCell(x, y);
grid.DeactivateCell(x, y);
```

---

### Input System

Base classes and utilities for Unity's Input System.

**Features:**
- **InputManager**: Centralized input management
- **InputReaderBase**: Base class for input handling
- **InputBindingDisplay**: Display input bindings in UI
- **ActionPromptVisual**: Visual representation of input actions
- **InputDeviceType**: Device type detection (Keyboard/Mouse, Gamepad, Touch)

**Usage:**
```csharp
public class MyInputReader : InputReaderBase
{
    private InputAction _jumpAction;

    protected override void Awake()
    {
        base.Awake();
        _jumpAction = PlayerInput.actions.FindAction("Jump");
    }
}
```

---

### Menu System

A robust menu navigation and UI animation system.

**Features:**
- **MenuManager**: Manages menu screen transitions
- **Screen**: Base class for menu screens
- **ScreenNavigation**: Navigation between screens
- **ScreenAnimation**: Animated screen transitions
- **SelectableAnimator**: Animate UI selectable elements
- **SelectableTextAnimator**: Animated text for UI selections

**Usage:**
```csharp
// Navigate to a screen
MenuManager.Instance.OpenScreen("MainMenu");

// Go back to previous screen
MenuManager.Instance.GoBack();
```

---

### Mobile Haptics

Cross-platform mobile haptic feedback system.

**Features:**
- **MobileHaptics**: Trigger haptic feedback on iOS and Android
- **MobileHapticsSetupWindow**: Editor setup window

**Usage:**
```csharp
// Trigger haptic feedback
MobileHaptics.TriggerHaptic(HapticType.Light);
MobileHaptics.TriggerHaptic(HapticType.Medium);
MobileHaptics.TriggerHaptic(HapticType.Heavy);
```

---

### Object Pooling System

High-performance object pooling to reduce instantiation overhead.

**Features:**
- **ObjectPooler**: Main pooling manager
- **Pool**: Individual object pool
- **IPoolable**: Interface for poolable objects
- **PoolableAudioSource**: Pooled audio sources
- **PoolableParticleSystem**: Pooled particle systems
- **PoolableAutoReturn**: Automatically return objects to pool

**Usage:**
```csharp
// Get object from pool
GameObject obj = ObjectPooler.Instance.Get("BulletPool", position, rotation);

// Return object to pool
ObjectPooler.Instance.Return(obj, "BulletPool");
```

---

### Rewind System

Time rewind system for gameplay mechanics or debugging.

**Features:**
- **RewindManager**: Manages recording and playback
- **Rewindable**: Base component for rewindable objects
- **RewindableTransform**: Record and rewind transform changes
- **RewindableRigidbody**: Record and rewind physics
- **RewindableAudioSource**: Rewind audio playback
- **FrameRecordContainer**: Stores frame data

**Usage:**
```csharp
// Start recording
RewindManager.Instance.StartRecording();

// Rewind
RewindManager.Instance.Rewind();

// Stop rewind
RewindManager.Instance.StopRewind();
```

---

### ScriptableObject System

ScriptableObject-based systems for data management and events.

#### Audio Event System
ScriptableObject-based audio event system for flexible sound management.

```csharp
// Create an audio event asset
[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class SOAudioEvent : ScriptableObject
{
    public AudioClip[] clips;
    public Vector2 volumeRange = new Vector2(1f, 1f);
    public Vector2 pitchRange = new Vector2(1f, 1f);

    public void Play(AudioSource source)
    {
        // Plays random clip with random volume/pitch
    }
}

// Usage in your scripts
[SerializeField] private SOAudioEvent footstepSound;
[SerializeField] private AudioSource audioSource;

footstepSound.Play(audioSource);
```

#### Event System
**SOEvent**: ScriptableObject-based event system for decoupled communication.

#### Value System
ScriptableObject wrappers for various value types:
- **SOFloat**, **SOInt**, **SOBool**, **SOString**
- **SOVector3**, **SOVector4**, **SOQuaternion**
- **SOColor**, **SOAnimationCurve**, **SOLayerMask**

---

### SDF (Signed Distance Fields)

Runtime SDF shape generation for procedural graphics.

**Features:**
- **SDFShapeBase**: Base class for SDF shapes
- **SDFCircle**, **SDFRectangle**, **SDFRing**: Basic shapes
- **SDFPolygon**, **SDFCross**, **SDFLine**: Additional shapes
- **SDFHeart**: Complex shape example

**Usage:**
Add SDF components to GameObjects to generate procedural shapes at runtime using signed distance field techniques.

---

### Springs

Physics-based spring animation system for smooth, natural motion.

#### Spring Types
- **FloatSpring**: Animate float values
- **Vector3Spring**: Animate Vector3 values
- **QuaternionSpring**: Animate rotations
- **ColorSpring**: Animate colors

#### Spring Components
- **TransformSpring**: Spring-based transform animation
- **RectSpring**: Spring animations for RectTransform
- **VisualizedSpring**: Debug visualization for springs

#### Usage

```csharp
// Float spring
FloatSpring spring = new FloatSpring(frequency: 2f, damping: 0.5f);
spring.Target = 100f;
float value = spring.Update(Time.deltaTime);

// Vector3 spring for smooth following
Vector3Spring positionSpring = new Vector3Spring(3f, 0.8f);
positionSpring.Target = targetPosition;
transform.position = positionSpring.Update(Time.deltaTime);

// Quaternion spring for smooth rotation
QuaternionSpring rotationSpring = new QuaternionSpring(4f, 0.7f);
rotationSpring.Target = targetRotation;
transform.rotation = rotationSpring.Update(Time.deltaTime);
```

---

### VFX & Transition System

Powerful visual effects and screen transition system.

**Features:**
- **VFXManager**: Manages visual effects
- **TransitionManager**: Handles screen transitions
- **EffectBase**: Base class for custom effects
- **EffectSequence**: Chain multiple effects
- **PropertyAnimation**: Animate shader properties

**Built-in Effects:**
- **SetFullscreenColor**: Fullscreen color overlay
- **SetFullscreenShaderTransition**: Custom shader transitions
- **SetVignette**: Post-processing vignette control
- Additional legacy effects for camera, icons, and post-processing

**Usage:**
```csharp
// Play a transition effect
TransitionManager.Instance.PlayTransition("FadeToBlack");

// Create effect sequence
EffectSequence sequence = new EffectSequence();
sequence.AddEffect(fadeEffect);
sequence.AddEffect(vignetteEffect);
sequence.Play();
```

---

## Utilities

An extensive collection of editor and runtime utilities designed to enhance your Unity workflow and boost productivity.

### Attributes

Custom property attributes for enhanced Inspector functionality.

#### InfoBox Attribute
Display informational boxes in the Inspector above fields.

```csharp
[InfoBox("This is an important setting!")]
[SerializeField] private float criticalValue;
```

#### ReadOnly Attribute
Make fields visible but non-editable in the Inspector.

```csharp
[ReadOnly]
[SerializeField] private int currentScore;
```

#### Separator Attribute
Add visual separators between fields in the Inspector.

```csharp
[Separator("Player Settings")]
[SerializeField] private float moveSpeed;
```

#### Preview Attribute
Preview assets directly in the Inspector.

```csharp
[Preview]
[SerializeField] private Sprite characterSprite;
```

#### Foldout Attribute
Group related fields under a foldout in the Inspector.

```csharp
[Foldout("Advanced Settings")]
[SerializeField] private bool enableDebug;
```

#### Inline Attribute
Edit ScriptableObject fields inline in the Inspector without creating separate assets.

```csharp
[SerializeField, Inline]
private MyScriptableObject inlineData;
```

#### Conditional Attributes
Show, hide, enable, or disable fields based on conditions.

```csharp
[SerializeField] private bool useCustomColor;
[ShowIf("useCustomColor")] [SerializeField] private Color customColor;

[SerializeField] private bool isInvincible;
[HideIf("isInvincible")] [SerializeField] private int health;

[SerializeField] private bool allowEditing;
[EnableIf("allowEditing")] [SerializeField] private string playerName;

[SerializeField] private bool isLocked;
[DisableIf("isLocked")] [SerializeField] private float value;
```

#### ComponentHeaderButton Attribute
Add custom buttons to component headers in the Inspector.

```csharp
[ComponentHeaderButton("Reset", nameof(ResetToDefaults))]
public class MyComponent : MonoBehaviour
{
    private void ResetToDefaults()
    {
        // Reset logic
    }
}
```

#### Button Attribute
Add inspector buttons that call methods with a single click.

```csharp
public class TestScript : MonoBehaviour
{
    [Button]
    public void TestMethod()
    {
        Debug.Log("Button clicked!");
    }

    [Button(ButtonPlayMode.PlayModeOnly)]
    public void RuntimeOnly()
    {
        // Only shows in play mode
    }

    [Button(ButtonPlayMode.EditModeOnly)]
    public void EditorOnly()
    {
        // Only shows in edit mode
    }
}
```

---

### Asset Selector

Browse and select assets from specific project folders.

#### PrefabSelector
Select prefabs from specific folders with filtering.

```csharp
[SerializeField, PrefabSelector("Assets/Prefabs/Characters")]
private GameObject characterPrefab;

// Lock to specific folder only
[SerializeField, PrefabSelector("Assets/Prefabs/Weapons", LockToFilter = true)]
private GameObject weaponPrefab;
```

#### SOSelector
Select ScriptableObjects from specific folders.

```csharp
[SerializeField, SOSelector("Assets/Data")]
private MyScriptableObject data;
```

---

### AutoGet System

Automatically populate serialized field references using attributes. Eliminates manual dragging and dropping of components.

#### Attributes Available
- **AutoGetSelf**: Get component from the same GameObject
- **AutoGetChildren**: Get component from children
- **AutoGetParent**: Get component from parent
- **AutoGetScene**: Find component in the scene
- **AutoGetAsset**: Find asset in the project

#### Usage Examples

```csharp
public class PlayerController : MonoBehaviour
{
    // Automatically gets Rigidbody from same GameObject
    [SerializeField, AutoGetSelf]
    private Rigidbody rb;

    // Automatically gets Animator from children
    [SerializeField, AutoGetChildren]
    private Animator animator;

    // Automatically gets Transform from parent
    [SerializeField, AutoGetParent]
    private Transform parentTransform;

    // Find GameManager in the scene
    [SerializeField, AutoGetScene]
    private GameManager gameManager;
}
```

#### Auto Get Settings
Configure auto-populate behavior through the project settings:
- Auto-populate on script creation
- Auto-populate on validation
- Validation warnings for missing references

---

### Audio Preview

Preview audio clips directly in the Inspector without entering play mode.

---

### Better Transform Editor

Enhanced transform component editor with additional features:
- Reset individual position/rotation/scale components
- Copy/paste transform values
- Randomize transform values
- Snap to grid functionality

---

### Better Unity Event

Improved UnityEvent property drawer with better visual organization and ease of use.

---

### Chance List

A list where each element has an associated probability weight for random selection.

```csharp
[SerializeField] private ChanceList<GameObject> enemySpawns;
[SerializeField] private ChanceList<Color> rarityColors;

// Get random item based on weights
GameObject randomEnemy = enemySpawns.GetRandom();
Color randomColor = rarityColors.GetRandom();
```

---

### Cinemachine Extensions

Extensions for Unity's Cinemachine camera system.

**Features:**
- **CinemachineRotationOffsetExtension**: Adds additional rotation offset to Cinemachine virtual cameras
- **CinemachineImpulseSourceExtensions**: Helper extensions for working with Cinemachine impulse sources

**Usage:**
Add the `CinemachineRotationOffsetExtension` component to your Cinemachine virtual camera to apply custom rotation offsets programmatically.

---

### Component Dragger

Drag and reorder components in the Inspector for better organization.

---

### Custom Fields

Specialized field types for the Inspector.

#### SceneField
Reference scenes safely in the Inspector with validation.

```csharp
[SerializeField] private SceneField mainMenuScene;

// Load the scene
SceneManager.LoadScene(mainMenuScene.SceneName);
```

#### TagField
Dropdown selector for Unity tags.

```csharp
[SerializeField] private TagField enemyTag;

if (collision.gameObject.CompareTag(enemyTag.Tag))
{
    // Handle enemy collision
}
```

#### SortingLayerField
Dropdown selector for sorting layers.

```csharp
[SerializeField] private SortingLayerField uiLayer;
renderer.sortingLayerID = uiLayer.LayerID;
```

#### PositionField
Visual position field with scene picking support.

```csharp
[SerializeField] private PositionField spawnPoint;
Instantiate(prefab, spawnPoint.Position, Quaternion.identity);
```

#### OptionalField<T>
Wrapper for optional values (similar to C# Nullable).

```csharp
[SerializeField] private OptionalField<float> customSpeed;

if (customSpeed.Enabled)
{
    speed = customSpeed.Value;
}
```

#### NoteField
Add editable or read-only notes in the Inspector.

```csharp
[SerializeField] private NoteField designNote =
    new NoteField("Remember to balance this value!", false);
```

#### AnimatorStateField
Select animator states from a dropdown.

```csharp
[SerializeField] private AnimatorStateField idleState;
animator.Play(idleState.StateName);
```

#### AnimatorTriggerField
Select animator triggers from a dropdown.

```csharp
[SerializeField] private AnimatorTriggerField jumpTrigger;
animator.SetTrigger(jumpTrigger.TriggerName);
```

#### Ranged Values
Min-max value ranges with visual editor.

```csharp
[SerializeField] private RangedFloat damageRange;
[SerializeField, MinMaxRange(0, 100)] private RangedInt healthRange;

// Get random value in range
float damage = damageRange.Random;
int health = healthRange.Random;

// Lerp between min and max
float interpolated = damageRange.Lerp(0.5f);
```

---

### Extensions

Extensive extension methods for Unity types.

#### Vector Extensions

```csharp
// Vector2 Extensions
Vector2 v = new Vector2(3, 4);
v.WithX(5);           // Returns (5, 4)
v.WithY(10);          // Returns (3, 10)
v.AddX(2);            // Returns (5, 4)
v.AddY(-1);           // Returns (3, 3)

// Vector3 Extensions
Vector3 pos = new Vector3(1, 2, 3);
pos.WithX(10);        // Returns (10, 2, 3)
pos.WithY(20);        // Returns (1, 20, 3)
pos.WithZ(30);        // Returns (1, 2, 30)
pos.RemoveY();        // Returns (1, 0, 3) - sets Y to 0
pos.Flat();           // Returns Vector2(1, 3) - XZ plane
```

#### Transform Extensions

```csharp
Transform t = transform;

// Destroy all children
t.DestroyChildren();

// Reset transform
t.ResetTransformation();

// Get all children
Transform[] children = t.GetChildren();

// Find child by name recursively
Transform child = t.FindDeepChild("ChildName");

// Set position/rotation/scale individually
t.SetPositionX(10f);
t.SetPositionY(5f);
t.SetLocalScaleX(2f);
```

#### GameObject Extensions

```csharp
GameObject obj = gameObject;

// Get or add component
Rigidbody rb = obj.GetOrAddComponent<Rigidbody>();

// Check if has component
bool hasCollider = obj.HasComponent<Collider>();

// Get component in parent or children
MeshRenderer renderer = obj.GetComponentInParentOrChildren<MeshRenderer>();

// Set layer recursively
obj.SetLayerRecursively(LayerMask.NameToLayer("Enemy"));
```

#### Rigidbody Extensions

```csharp
Rigidbody rb = GetComponent<Rigidbody>();

// Change velocity components
rb.ChangeVelocityX(5f);
rb.ChangeVelocityY(10f);

// Set velocity direction
rb.SetVelocityDirection(Vector3.forward, 20f);
```

#### Material & Renderer Extensions

```csharp
Material mat = renderer.material;

// Fade alpha
mat.SetAlpha(0.5f);

// Change color properties
mat.ChangeColor(Color.red);
mat.ChangeEmissionColor(Color.yellow);

// Renderer extensions
renderer.FadeIn(duration: 1f);
renderer.FadeOut(duration: 1f);
```

#### Color Extensions

```csharp
Color color = Color.red;

// Modify components
color.WithAlpha(0.5f);
color.WithR(0.2f);
color.WithG(0.8f);

// Convert to hex
string hex = color.ToHex();

// Parse from hex
Color parsed = ColorExtensions.FromHex("#FF5733");
```

#### Camera Extensions

```csharp
Camera cam = Camera.main;

// Get world boundaries
Bounds bounds = cam.GetCameraBounds();

// Check if position is visible
bool visible = cam.IsPositionVisible(worldPos);

// World to viewport safe
Vector3 viewport = cam.WorldToViewportPointSafe(worldPos);
```

#### String Extensions

```csharp
string text = "Hello World";

// Rich text helpers
text.Bold();
text.Italic();
text.Color(Color.red);
text.Size(24);

// Validation
bool isValid = "user@email.com".IsValidEmail();

// Truncate
string short = "Very long text".Truncate(10); // "Very long..."
```

#### List Extensions

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

// Get random element
int random = numbers.GetRandom();

// Shuffle
numbers.Shuffle();

// Remove random
int removed = numbers.RemoveRandom();

// For each with index
numbers.ForEachWithIndex((item, index) =>
{
    Debug.Log($"{index}: {item}");
});
```

#### Other Extensions
- **PropertyPathExtensions**: SerializedProperty path manipulation
- **SelectableExtensions**: UI Selectable helpers
- **MonoBehaviourExtensions**: Component utilities
- **EditorExtensions**: Editor-specific helpers
- **NumberExtensions**: Numeric utilities and formatting

---

### Play From Camera

Editor utility to start play mode from the scene view camera position.

---

### Save In Play Mode

Allows saving changes made in play mode to persist after exiting play mode.

---

### ScriptableObject Editor

Enhanced editor for ScriptableObjects with improved workflow.

---

### Serializable Selector

Polymorphic serialization with dropdown selector for derived types.

```csharp
[System.Serializable]
public abstract class Ability
{
    public float cooldown;
}

[System.Serializable]
public class FireballAbility : Ability
{
    public float damage;
}

[System.Serializable]
public class HealAbility : Ability
{
    public float healAmount;
}

// In your MonoBehaviour
[SerializeReference, SerializableSelector]
private Ability ability;

[SerializeReference, SerializableSelector]
private List<Ability> abilities;
```

#### Selector Attributes

```csharp
[SerializableSelectorName("Fire Spell", "Magic/Offensive")]
[SerializableSelectorTooltip("Casts a fireball")]
[SerializableSelectorAllowOnce] // Can only add once to list
public class FireballAbility : Ability { }
```

---

### Serialized Interface

Serialize interface references in the Inspector.

```csharp
public interface IDamageable
{
    void TakeDamage(float amount);
}

// Serialize interface reference
[SerializeField]
private InterfaceReference<IDamageable> target;

// Use it
target.Interface?.TakeDamage(10f);

// Require interface on MonoBehaviour field
[SerializeField, RequireInterface(typeof(IDamageable))]
private MonoBehaviour damageableObject;
```

---

### Toolbar Extensions

Custom toolbar additions for the Unity editor:
- **ToolbarProjectButtons**: Quick access project buttons
- **ToolbarReloadAssembly**: Reload assemblies button
- **ToolbarTimescale**: Time scale control in toolbar

---
