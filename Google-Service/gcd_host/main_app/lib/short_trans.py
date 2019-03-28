# -*- coding: utf-8 -*-
# Reference: https://cloud.google.com/speech-to-text/docs/sync-recognize#speech-sync-recognize-python

from __future__ import print_function
import io

from google.cloud import speech
from google.cloud.speech import enums
from google.cloud.speech import types

def transcribe_file(speech_file):
    """Transcribe the given audio file."""
    client = speech.SpeechClient()

    with io.open(speech_file, 'rb') as audio_file:
        content = audio_file.read()

    audio = types.RecognitionAudio(content=content)
    config = types.RecognitionConfig(
        encoding=enums.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=16000,
        language_code='en-US')

    response = client.recognize(config, audio)

    # Each result is for a consecutive portion of the audio. Iterate through
    # them to get the transcripts for the entire audio file.
    transcripts = []
    for result in response.results:
        # The first alternative is the most likely one for this portion.
        trans = result.alternatives[0].transcript
        print(u'Transcript: {}'.format(trans))
        transcripts.append(trans)

    return transcripts

if __name__ == '__main__':
    print('Testing short_trans method...')
    transcribe_file('speech-test-data.wav')
