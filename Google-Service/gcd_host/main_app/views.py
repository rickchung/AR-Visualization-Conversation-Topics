# -*- coding: utf-8 -*-
from __future__ import unicode_literals
from django.shortcuts import render
from django.http import HttpResponse, JsonResponse

from tempfile import NamedTemporaryFile

from .forms import UploadFileForm
from .lib.short_trans import transcribe_file

from .lib.w2v import javaw2v

# Create your views here.
def index(request):
    return HttpResponse("Sorry man. Nothing is here")

def upload_file(request):
    if request.method == 'POST':
        form = UploadFileForm(request.POST, request.FILES)

        if form.is_valid():
            uploaded_file = request.FILES['file']
            tmp_filename = None

            # Save the uploaded file temporarily
            with NamedTemporaryFile() as f:
                for chunk in uploaded_file.chunks():
                    f.write(chunk)
                tmp_filename = f.name
                
                # Call Google cloud transcription service
                results = transcribe_file(tmp_filename)

                # Find relevant keywords (w2v)
                tokens = javaw2v.doc_to_tokens(' '.join(results))
                topics, keywords = javaw2v.query_topics_from_raw(tokens)
                subtopics = [[]]
                
                # Find relevant examples
                examples = [[]]
                

            return JsonResponse({
                'msg': 'valid', 
                'transcript': results,
                'keywords': keywords,
                'topics': topics,
                'subtopics': subtopics,
                'examples': examples,
            })

        else:
            return JsonResponse({
                'msg': 'the form is not valid.', 
                'errors': str(form.errors)})
    else:
        form = UploadFileForm()
    return render(request, 'main_app/upload.html', {'form': form})

