# -*- coding: utf-8 -*-
from __future__ import unicode_literals
from django.shortcuts import render
from django.http import HttpResponse, JsonResponse

from tempfile import NamedTemporaryFile

from .forms import UploadFileForm
from .lib.short_trans import transcribe_file

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

            return JsonResponse({
                'msg': 'valid', 
                'transcript': results})

        else:
            return JsonResponse({
                'msg': 'the form is not valid.', 
                'errors': str(form.errors)})
    else:
        form = UploadFileForm()
    return render(request, 'main_app/upload.html', {'form': form})

