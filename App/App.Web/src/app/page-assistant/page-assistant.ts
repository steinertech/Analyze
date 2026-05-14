import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ServerApi } from '../server-api';

@Component({
  selector: 'app-page-assistant',
  imports: [FormsModule],
  templateUrl: './page-assistant.html',
  styleUrl: './page-assistant.css'
})
export class PageAssistant {
  private serverApi = inject(ServerApi);

  messages = signal<string[]>([]);
  inputText = '';

  async send() {
    const text = this.inputText.trim();
    this.inputText = '';
    const response = await this.serverApi.commandAssistant(text);
    if (response?.text) {
      this.messages.update(list => [...list, response.text!]);
    }
  }
}
