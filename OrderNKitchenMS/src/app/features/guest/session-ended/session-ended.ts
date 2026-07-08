import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-session-ended',
  imports: [CommonModule],
  templateUrl: './session-ended.html',
  styleUrl: './session-ended.css',
})
export class SessionEndedComponent implements OnInit {
  public isDirectAccess: boolean = true;
  public tableNumber: number = 0;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    const endedBySession = navigation?.extras?.state?.['endedBySession'] === true;

    if (endedBySession) {
      this.isDirectAccess = false;
      this.tableNumber = navigation?.extras?.state?.['tableNumber'] ?? 0;
    }
  }

  ngOnInit(): void { }
}
