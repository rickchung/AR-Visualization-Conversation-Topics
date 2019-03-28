from django.conf.urls import url, include

from . import views

urlpatterns = [
    url(r'upload/', views.upload_file, name='upload_flie'),
    url(r'index/', views.index, name='index'),
]
