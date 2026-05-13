using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundTreeChop : MonoBehaviour
{
	[Header("Chop Sound Settings")]
	[Tooltip("Assign exactly 4 chop sound clips here.")]
	public AudioClip[] chopSounds = new AudioClip[4];

	private AudioSource audioSource;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void PlayRandomChopSound()
	{
		if (!CompareTag("Tree")) return;
		if (audioSource == null || chopSounds == null || chopSounds.Length == 0) return;

		int index = Random.Range(0, chopSounds.Length);
		AudioClip selectedClip = chopSounds[index];

		if (selectedClip != null)
		{
			audioSource.PlayOneShot(selectedClip);
		}
	}
}
